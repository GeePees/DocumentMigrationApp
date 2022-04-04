using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Compression;

/* Document Migration App
 * Created this app to read files from document directory and convert them to base64 or binary and finally store them to the SQL server database
 * 01-03-2022
 */

namespace AU_Document
{
    public partial class DocMig : Form
    {
        public DocMig()
        {
            InitializeComponent();

            // Hardcoded user inputs 
            textBox1.Text = @"D:\Data_Migration\InvestorData\Doc\OV\OV_SIF_Outbound_20220301_Batch1";
            //textBox1.Text = @"Select the directory...";
            textBox2.Text = @"SQL Server Name";
            textBox3.Text = "Main Database Name";
            textBox6.Text = "Document Vault Dtatabase Name";
            comboBox1.Text = "Windows Auth"; //"SQL Server Auth";
            textBox4.Text = "username";
            textBox5.Text = "password";
            textBox5.PasswordChar = '*';
            timeOut = "90";
            // progress bar label - begin
            label9.Text = "";
        }

        // declarations
        string[] allfiles = null;
        string[,] allfiles2d;
        private string connString;
        private string databasename;
        private DataTable dataTable;
        private DataTable convertedDataTable;
        string timeOut;

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            progressBar1.Visible = true;
            // progress bar label - begin
            label9.Text = "Reading Documents...";

            // read staging document matadata - read database
            ReadAllDocMataData();

            // reading all documents in the given directry
            ReadAllDocuments();

            // progress bar label - begin
            label9.Text = "Converting to Base64...";
            // Update Staging record if phiysical file name exists in MetaData staging table
            UpdateStagingRecords();

            // progress bar label - begin
            label9.Text = "Saving Documents...";
            // Convert staging data to the Docuemnt vault database
            ConvertStagingDatatoVault();

            // end of process
            linkLabel1.Enabled = false;
            // progress bar label - begin
            label9.Text = convertedDataTable.Rows.Count.ToString() + " documents inserted...";
        }

        // set connection string
        private void SetDatabaseConfig(string dbname)
        {
            if (dbname == "Main")
            {
                databasename = textBox3.Text;
            }
            else if (dbname == "Vault")
            {
                databasename = textBox6.Text;
            }

            connString = @"data source=" + textBox2.Text + "; initial catalog=" + databasename;
            if (comboBox1.Text == "SQL Server Auth")
            {
                connString = connString + "; user id=" + textBox4.Text + "; password=" + textBox5.Text;
            }
            else if (comboBox1.Text == "Windows Auth")
            {
                connString = connString + "; Integrated Security=SSPI";
            }
            connString = connString + "; Connection Timeout=" + timeOut + "; MultipleActiveResultSets=True;";
        }

        // read Document MataData from the staging table
        public void ReadAllDocMataData()
        {
            // change config to support main database
            SetDatabaseConfig("Main");

            dataTable = new DataTable();

            try
            {
                string query = "select * from staging.AU_DocMataData /*where SEQ_NO > 116590*/ order by SEQ_NO";

                SqlConnection conn = new SqlConnection(connString);
                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();

                // create data adapter
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                // this will query your database and return the result to your datatable
                da.Fill(dataTable);

                conn.Close();
                da.Dispose();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        // read every end points in the given file directory
        public void ReadAllDocuments()
        {
            // Set Minimum to 1 to represent the first file being copied.
            progressBar1.Minimum = 1;
            // Set the initial value of the ProgressBar.
            progressBar1.Value = 1;
            // Set the Step property to a value of 1 to represent each file being copied.
            progressBar1.Step = 1;

            try
            {
                allfiles = Directory.GetFiles(textBox1.Text, "*.*", SearchOption.AllDirectories);

                // add document data to this 2d array temporarily
                allfiles2d = new string[allfiles.Length, 5];
                var i = 0;
                // Set Maximum to the total number of files to copy.
                progressBar1.Maximum = allfiles.Length;

                foreach (string files in allfiles)
                {
                    // add path + name of the file
                    allfiles2d[i, 0] = files;
                    // add path without name of the file
                    allfiles2d[i, 1] = files.Substring(0, (files.Length - Reverse(files).IndexOf('\\')));
                    // add file name without path
                    allfiles2d[i, 2] = files.Substring(allfiles2d[i, 1].Length);
                    // add file extension
                    allfiles2d[i, 3] = files.Substring(files.Length - Reverse(files).IndexOf('.'));
                    // convert doc to the 64 bit and store 
                    //allfiles2d[i, 4] = ToBase64String(files);  // Removed cos too much memory consumed.                  

                    // Perform the increment on the ProgressBar.
                    progressBar1.PerformStep();

                    // increment
                    i++;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            // progress bar label - end
            //label9.Text = "";
        }

        // this function will check if the Physical file name exists in the MataData staging table and update encode if exists
        private void UpdateStagingRecords()
        {
            // change config to support main database
            SetDatabaseConfig("Main");
            // Set Minimum to 1 to represent the first file being copied.
            progressBar1.Minimum = 1;
            // Set the initial value of the ProgressBar.
            progressBar1.Value = 1;
            // Set the Step property to a value of 1 to represent each file being copied.
            progressBar1.Step = 1;
            // Set Maximum to the total number of files to copy.
            progressBar1.Maximum = dataTable.Rows.Count;

            foreach (DataRow rowST in dataTable.Rows)
            {
                string fileNamehMata = rowST["OutputFileName"].ToString();

                for (int i = 0; i < allfiles2d.GetLength(0); i++)
                {
                    // update staging table if physical file name and metadata file name matach
                    if (allfiles2d[i, 2] == fileNamehMata)
                    {
                        try
                        {
                            // storing document path and decode values to the staging table
                            SqlConnection conn = new SqlConnection(connString);
                            SqlCommand cmd = new SqlCommand("Update staging.AU_DocMataData set FullPath = @rFullPath" +
                                                                                  ", PathWithoutName = @rPathWithoutName" +
                                                                                  ", NameWithoutPath = @rNameWithoutPath" +
                                                                                  ", FileExe = @rFileExe" +
                                                                                  ", FileDecode = @rFileDecode " +
                                                                                  "where trim(OutputFileName) = @rOutputFileName", conn);
                            conn.Open();

                            // document mata data and decode values 
                            cmd.Parameters.Add("@rFullPath", SqlDbType.VarChar).Value = allfiles2d[i, 0];
                            cmd.Parameters.Add("@rPathWithoutName", SqlDbType.VarChar).Value = allfiles2d[i, 1];
                            cmd.Parameters.Add("@rNameWithoutPath", SqlDbType.VarChar).Value = allfiles2d[i, 2];
                            cmd.Parameters.Add("@rFileExe", SqlDbType.VarChar).Value = allfiles2d[i, 3];
                            cmd.Parameters.Add("@rFileDecode", SqlDbType.Text).Value = ""; //allfiles2d[i, 4];                            
                            cmd.Parameters.Add("@rOutputFileName", SqlDbType.VarChar).Value = allfiles2d[i, 2].ToString().Trim();

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
            }
        }

        // Converting staging data to the ARMNet interface table | decoded document value will be storing in the given vault database
        private void ConvertStagingDatatoVault()
        {
            // Load converted doc data
            ReadConvertedStagingData();

            // Save document data to Document Master table in Main database
            SaveToDocumentMaster();

            // save document encode value in area master in Vault database            
            SaveToDocumentAreaPackage(radioButton2.Checked);
        }

        // this function create an array with all valid converted document data that ready to insert to ARMNet interface tables.
        private void ReadConvertedStagingData()
        {
            // change config to support main database
            SetDatabaseConfig("Main");

            convertedDataTable = new DataTable();

            try
            {
                string query = "SELECT '{' + lower(cast(newid() as varchar(38))) + '}' as [DMR_ID],'{}' as [Ownership], 'Conversion_AU-D | Migrated on ' + lower(cast(cast(getdate() as date) as varchar(10))) as [Note], YMR_IDLink_ARMNet[DMR_IDLink_Code], " +
                               "XLK_ID[DMR_IDLink_XLK], '{5385c7db-840d-4b62-bf1b-a76902e5f2f8}'[DMR_IDLink_CreatedBy], '{5457433b-48a9-468b-b58d-5b98f83ea995}' [DMR_IDLink_XSYSdoc], 0 [DMR_DocLinkType], cast(RequestDate as datetime)[DMR_DateCreated], " +
                               "substring(PathWithoutName, 0, len(PathWithoutName))[DMR_UNCPath], substring(NameWithoutPath, 0, len(NameWithoutPath) - len(FileExe))[DMR_FileOriginalName], substring(NameWithoutPath, 0, len(NameWithoutPath) - len(FileExe))[DMR_FileDisplayName]," +
                               "FileExe[DMR_FileDisplayExtension], XLK_AlternateDetail[DMR_FileSubject], '{' + lower(cast(newid() as varchar(38))) + '}' as [DPK_ID], FileExe[DPK_FileDisplayExtension],FileDecode[DPK_Base64Data] " +
                               "from staging.AU_DocMataData join iO_Keys_MasterReference on YMR_IDLink_Foreign = cast(cast(AccountNumber as bigint) as varchar(40)) left join iO_Control_LinkMaster on replace(XLK_AlternateDetail, ' ', '') = [Description] and XLK_IDLink_XLKc = 14 " +
                               " where YMR_IDLink_XFK = '{ef3baea0-8d36-4c5a-8a2d-6ce9e119335b}' and FileDecode is not null";

                SqlConnection conn = new SqlConnection(connString);
                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();

                // create data adapter
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                // this will query your database and return the result to your datatable
                da.Fill(convertedDataTable);

                conn.Close();
                da.Dispose();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        // save matadata to the document reference table
        private void SaveToDocumentMaster()
        {
            // change config to support main database
            SetDatabaseConfig("Main");
            // Set Minimum to 1 to represent the first file being copied.
            progressBar1.Minimum = 1;
            // Set the initial value of the ProgressBar.
            progressBar1.Value = 1;
            // Set the Step property to a value of 1 to represent each file being copied.
            progressBar1.Step = 1;
            // Set Maximum to the total number of files to copy.
            progressBar1.Maximum = convertedDataTable.Rows.Count;

            // Remove previously migrated data from io_Document_MasterReference table
            RemoveMigratedDataFromARMTable("Main", "iO_Document_MasterReference", "DMR");

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
        private void SaveToDocumentAreaPackage(Boolean docStoreType)
        {
            // change config to support vault database
            SetDatabaseConfig("Vault");
            // Set Minimum to 1 to represent the first file being copied.
            progressBar1.Minimum = 1;
            // Set the initial value of the ProgressBar.
            progressBar1.Value = 1;
            // Set the Step property to a value of 1 to represent each file being copied.
            progressBar1.Step = 1;
            // Set Maximum to the total number of files to copy.
            progressBar1.Maximum = convertedDataTable.Rows.Count;

            // Remove previously migrated data from io_Document_MasterReference table
            RemoveMigratedDataFromARMTable("Vault", "iO_Document_AreaPackage", "DPK");

            foreach (DataRow rowST in convertedDataTable.Rows)
            {
                try
                {
                    // insert document reference data
                    SqlConnection conn = new SqlConnection(connString);
                    SqlCommand cmd;

                    // decide the doc stores as base64 or binary
                    if (docStoreType) // True = Binary
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
                    if (radioButton1.Checked)
                    {
                        cmd.Parameters.Add("@DPK_Base64Data", SqlDbType.Text).Value = ToBase64String(rowST["DMR_UNCPath"].ToString() + "\\" + rowST["DMR_FileOriginalName"].ToString() + "." + rowST["DMR_FileDisplayExtension"].ToString()); //rowST["DPK_Base64Data"];
                    }

                    if (checkBox1.Checked && radioButton2.Checked)
                    {
                        cmd.Parameters.Add("@DPK_BinData", SqlDbType.VarBinary).Value = Compress(ImageToBinary(rowST["DMR_UNCPath"].ToString() + "\\" + rowST["DMR_FileOriginalName"].ToString() + "." + rowST["DMR_FileDisplayExtension"].ToString()));
                        cmd.Parameters.Add("@DPK_CompressionMode", SqlDbType.VarChar).Value = "gzip";
                    }
                    else if ((checkBox1.Checked == false) && radioButton2.Checked)
                    {
                        cmd.Parameters.Add("@DPK_BinData", SqlDbType.VarBinary).Value = ImageToBinary(rowST["DMR_UNCPath"].ToString() + "\\" + rowST["DMR_FileOriginalName"].ToString() + "." + rowST["DMR_FileDisplayExtension"].ToString());
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

        // reverse any string value
        public static string Reverse(string s)
        {
            char[] charArray = s.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }

        // convert filestream to base 64 string
        public static string ToBase64String(string fileName)
        {
            using (FileStream reader = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                byte[] buffer = new byte[reader.Length];
                reader.Read(buffer, 0, (int)reader.Length);
                reader.Close();
                return Convert.ToBase64String(buffer);
            }
        }

        // convert file to binary
        public static byte[] ImageToBinary(string imagePath)
        {
            FileStream fS = new FileStream(imagePath, FileMode.Open, FileAccess.Read);
            byte[] b = new byte[fS.Length];
            fS.Read(b, 0, (int)fS.Length);
            fS.Close();
            return b;
        }

        // compress the document binary before storing in ARMNet
        static byte[] Compress(byte[] data)
        {
            using (var compressedStream = new MemoryStream())
            using (var zipStream = new GZipStream(compressedStream, CompressionMode.Compress))
            {
                zipStream.Write(data, 0, data.Length);
                zipStream.Close();
                return compressedStream.ToArray();
            }
        }

        // remove migrated data from ARMNet's interface tables
        private void RemoveMigratedDataFromARMTable(string dbase, string table, string prefix)
        {
            // change config to support vault database
            SetDatabaseConfig(dbase);

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

        private void button1_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox1.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox1.Text == "Windows Auth")
            {
                textBox4.Enabled = false;
                textBox5.Enabled = false;
            }
            else
            {
                textBox4.Enabled = true;
                textBox5.Enabled = true;
            }
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            checkBox1.Enabled = false;
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            checkBox1.Enabled = true;
        }
    }
}
