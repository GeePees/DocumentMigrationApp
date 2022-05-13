using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace AU_Document
{
    class DocUpdate
    {
        DataTable convertedDataTable;

        // Converting staging data to the ARMNet interface table | decoded document value will be storing in the given vault database
        public DocUpdate(DocMataData objDocMataData, Boolean binaryRadio, Boolean base64Radio, Boolean fileCompressed, Boolean deleteAll, ProgressBar progressBar1, List<string> userInputs)
        {
            // Load converted doc data
            convertedDataTable = objDocMataData.ReadConvertedStagingData(userInputs, deleteAll);

            // Save document data to Document Master table in Main database
            SaveToDocumentMaster(progressBar1, userInputs, deleteAll);

            // save document encode value in area master in Vault database            
            SaveToDocumentAreaPackage(binaryRadio, base64Radio, fileCompressed, deleteAll, progressBar1, userInputs);
        }

        // save matadata to the document reference table
        private void SaveToDocumentMaster(ProgressBar progressBar1, List<string> userInputs, Boolean deleteAll)
        {
            // change config to support main database
            DataBaseConfig objDBCongig = new DataBaseConfig();
            string connString = objDBCongig.GetDatabaseConfig("Main", userInputs);
            // reset ProgressBar
            Utility objUtility = new Utility();
            objUtility.ResetProgressBar(progressBar1);
            // Set Maximum to the total number of files to copy.
            progressBar1.Maximum = convertedDataTable.Rows.Count;

            // Remove previously migrated data from io_Document_MasterReference table
            if (deleteAll)
            {
                RemoveMigratedDataFromARMTable("Main", "iO_Document_MasterReference", "DMR", userInputs);
            }


            foreach (DataRow rowST in convertedDataTable.Rows)
            {
                try
                {
                    // insert document reference data
                    SqlConnection conn = new SqlConnection(connString);
                    SqlCommand cmd = new SqlCommand("insert into iO_Document_MasterReference (DMR_ID, DMR_Ownership, DMR_IDLink_Code, DMR_IDLink_XLK, DMR_IDLink_CreatedBy, DMR_IDLink_XSYSdoc, DMR_DocLinkType, DMR_DateCreated, DMR_UNCPath, DMR_FileOriginalName, DMR_FileDisplayName, DMR_FileDisplayExtension, DMR_FileSubject, DMR_Note)" +
                                                    "values(@DMR_ID, @DMR_Ownership, @DMR_IDLink_Code, @DMR_IDLink_XLK, @DMR_IDLink_CreatedBy, @DMR_IDLink_XSYSdoc, @DMR_DocLinkType, @DMR_DateCreated, @DMR_UNCPath, @DMR_FileOriginalName, @DMR_FileDisplayName, @DMR_FileDisplayExtension, @DMR_FileSubject, @DMR_Note)", conn);
                    conn.Open();

                    // document mata data and decode values 
                    cmd.Parameters.Add("@DMR_ID", SqlDbType.VarChar).Value = rowST["DMR_ID"].ToString();
                    cmd.Parameters.Add("@DMR_Ownership", SqlDbType.VarChar).Value = rowST["Ownership"].ToString();
                    cmd.Parameters.Add("@DMR_IDLink_Code", SqlDbType.VarChar).Value = rowST["DMR_IDLink_Code"].ToString();
                    cmd.Parameters.Add("@DMR_IDLink_XLK", SqlDbType.VarChar).Value = rowST["DMR_IDLink_XLK"].ToString();
                    cmd.Parameters.Add("@DMR_IDLink_CreatedBy", SqlDbType.VarChar).Value = rowST["DMR_IDLink_CreatedBy"].ToString();
                    cmd.Parameters.Add("@DMR_IDLink_XSYSdoc", SqlDbType.VarChar).Value = rowST["DMR_IDLink_XSYSdoc"].ToString();
                    cmd.Parameters.Add("@DMR_DocLinkType", SqlDbType.VarChar).Value = rowST["DMR_DocLinkType"].ToString();
                    cmd.Parameters.Add("@DMR_DateCreated", SqlDbType.DateTime).Value = rowST["DMR_DateCreated"].ToString();
                    cmd.Parameters.Add("@DMR_UNCPath", SqlDbType.VarChar).Value = rowST["DMR_UNCPath"].ToString();
                    cmd.Parameters.Add("@DMR_FileOriginalName", SqlDbType.VarChar).Value = rowST["DMR_FileOriginalName"].ToString();
                    cmd.Parameters.Add("@DMR_FileDisplayName", SqlDbType.VarChar).Value = rowST["DMR_FileDisplayName"].ToString();
                    cmd.Parameters.Add("@DMR_FileDisplayExtension", SqlDbType.VarChar).Value = rowST["DMR_FileDisplayExtension"].ToString();
                    cmd.Parameters.Add("@DMR_FileSubject", SqlDbType.VarChar).Value = rowST["DMR_FileSubject"].ToString();
                    cmd.Parameters.Add("@DMR_Note", SqlDbType.VarChar).Value = rowST["Note"].ToString();

                    cmd.ExecuteNonQuery();
                    conn.Close();
                }
                catch (Exception ex)
                {
                    throw ex;
                }

                // Perform the increment on the ProgressBar.
                progressBar1.PerformStep();
            }
        }

        // Save base64 to the document area package table
        private void SaveToDocumentAreaPackage(Boolean binaryRadio, Boolean base64Radio, Boolean fileCompressed, Boolean deleteAll, ProgressBar progressBar1, List<string> userInputs)
        {
            // change config to support vault database
            DataBaseConfig objDBCongig = new DataBaseConfig();
            string connString = objDBCongig.GetDatabaseConfig("Vault", userInputs); 
            // reset ProgressBar
            Utility objUtility = new Utility();
            objUtility.ResetProgressBar(progressBar1);
            // Set Maximum to the total number of files to copy.
            progressBar1.Maximum = convertedDataTable.Rows.Count;            

            // Remove previously migrated data from io_Document_MasterReference table
            if (deleteAll)
            {
                RemoveMigratedDataFromARMTable("Vault", "iO_Document_AreaPackage", "DPK", userInputs);
            }

            foreach (DataRow rowST in convertedDataTable.Rows)
            {
                try
                {
                    // insert document reference data
                    SqlConnection conn = new SqlConnection(connString);
                    SqlCommand cmd;

                    // decide the doc stores as base64 or binary
                    if (binaryRadio) // True = Binary
                    {
                        // binary
                        cmd = new SqlCommand("insert into iO_Document_AreaPackage (DPK_ID, DPK_Ownership, DPK_IDLink_DMR, DPK_FileDisplayExtension, DPK_BinData, DPK_Note, DPK_CompressionMode)" +
                                              " values(@DPK_ID, @DPK_Ownership, @DPK_IDLink_DMR, @DPK_FileDisplayExtension, @DPK_BinData, @DPK_Note, @DPK_CompressionMode)", conn);
                    }
                    else
                    {
                        // base64
                        cmd = new SqlCommand("insert into iO_Document_AreaPackage (DPK_ID, DPK_Ownership, DPK_IDLink_DMR, DPK_FileDisplayExtension, DPK_Base64Data, DPK_Note)" +
                                              " values(@DPK_ID, @DPK_Ownership, @DPK_IDLink_DMR, @DPK_FileDisplayExtension, @DPK_Base64Data, @DPK_Note)", conn);
                    }

                    conn.Open();

                    // document mata data and decode values 
                    cmd.Parameters.Add("@DPK_ID", SqlDbType.VarChar).Value = rowST["DPK_ID"].ToString();
                    cmd.Parameters.Add("@DPK_Ownership", SqlDbType.VarChar).Value = rowST["Ownership"].ToString();
                    cmd.Parameters.Add("@DPK_IDLink_DMR", SqlDbType.VarChar).Value = rowST["DMR_ID"].ToString();
                    cmd.Parameters.Add("@DPK_FileDisplayExtension", SqlDbType.VarChar).Value = rowST["DPK_FileDisplayExtension"].ToString();
                    // base64  checked
                    if (base64Radio)
                    {
                        cmd.Parameters.Add("@DPK_Base64Data", SqlDbType.Text).Value = objUtility.ToBase64String(rowST["DMR_UNCPath"].ToString() + "\\" + rowST["DMR_FileOriginalName"].ToString() + "." + rowST["DMR_FileDisplayExtension"].ToString()); //rowST["DPK_Base64Data"];
                    }

                    if (fileCompressed && binaryRadio)
                    {
                        cmd.Parameters.Add("@DPK_BinData", SqlDbType.VarBinary).Value = objUtility.Compress(objUtility.ImageToBinary(rowST["DMR_UNCPath"].ToString() + "\\" + rowST["DMR_FileOriginalName"].ToString() + "." + rowST["DMR_FileDisplayExtension"].ToString()));
                        cmd.Parameters.Add("@DPK_CompressionMode", SqlDbType.VarChar).Value = "gzip";
                    }
                    else if ((base64Radio == false) && binaryRadio)
                    {
                        cmd.Parameters.Add("@DPK_BinData", SqlDbType.VarBinary).Value = objUtility.ImageToBinary(rowST["DMR_UNCPath"].ToString() + "\\" + rowST["DMR_FileOriginalName"].ToString() + "." + rowST["DMR_FileDisplayExtension"].ToString());
                        cmd.Parameters.Add("@DPK_CompressionMode", SqlDbType.VarChar).Value = "";
                    }

                    cmd.Parameters.Add("@DPK_Note", SqlDbType.VarChar).Value = rowST["Note"].ToString();

                    cmd.ExecuteNonQuery();
                    conn.Close();
                }
                catch (Exception ex)
                {
                    throw ex;
                }

                // Perform the increment on the ProgressBar.
                progressBar1.PerformStep();
            }
        }

        // remove migrated data from ARMNet's interface tables
        private void RemoveMigratedDataFromARMTable(string dbase, string table, string prefix, List<string> userInputs)
        {
            // change config to support vault database
            DataBaseConfig objDBCongig = new DataBaseConfig();
            string connString = objDBCongig.GetDatabaseConfig(dbase, userInputs);

            try
            {
                // storing document path and decode values to the staging table
                SqlConnection conn = new SqlConnection(connString);
                SqlCommand cmd = new SqlCommand("delete from " + table + " where " + prefix + "_Note like 'Conversion_AU%'", conn);
                conn.Open();
                cmd.ExecuteNonQuery();
                conn.Close();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

    }
}
