using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Windows.Forms;

/* Document Migration App
 * Created this app to read files from document directory and convert them to base64 or binary and finally store them to the SQL server database
 * 01-03-2022
 */

namespace AU_Document
{
    public partial class DocMig : Form
    {
        // declarations        
        string[,] allfiles2d;
        private DataTable dataTable;

        public DocMig()
        {
            InitializeComponent();
            SetToolTips();

            // Hardcoded user inputs 
            DocumentDirectoryName = @"C:\Users\pivi.hapu\Downloads\2022";
            //textBox1.Text = @"Select the directory...";
            ServerName = @"VM-VIC";
            MainDatabaseName = "AU_DEV_Main";
            VaultDatabaseName = "AU_DEV_Vault";
            SQLAuthType = "SQL Server Auth"; // "Windows Auth";
            BatchLoadVersion = "Batch2";
            SQLUserName = "pivi.hapu";
            SQLPassword = "Getthru7";
            textBox5.PasswordChar = '*';
            // progress bar label - begin
            ProgressMessage = "";

        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            progressBar1.Visible = true;
            // progress bar label - begin
            ProgressMessage = "Reading Documents...";

            // read staging document matadata - read database
            DocMataData objDocMataData = new DocMataData();
            dataTable = objDocMataData.ReadAllDocMataData(GetTestUserInputsAsList());

            // reading all documents in the given directry
            DocFiles objDocFiles = new DocFiles();
            allfiles2d = objDocFiles.ReadAllDocuments(progressBar1, DocumentDirectoryName);

            // progress bar label - begin
            ProgressMessage = "Converting to Base64...";
            // Update Staging record if phiysical file name exists in MetaData staging table
            objDocMataData.UpdateStagingRecords(progressBar1, allfiles2d, GetTestUserInputsAsList(), BatchLoadVersion);

            // progress bar label - begin
            ProgressMessage = "Saving Documents...";
            // Convert staging data to the Docuemnt vault database
            DocUpdate objDocConvert = new DocUpdate(objDocMataData, GetBinaryRadio(), GetBase64Radio(), GetCompressedCheckBox(), GetDeleteAllCheckBox(), progressBar1, GetTestUserInputsAsList());

            // end of process
            linkLabel1.Enabled = false;
            // progress bar label - begin
            ProgressMessage = objDocMataData.ContertedRecordCount() + " documents inserted...";
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

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (((System.Windows.Forms.CheckBox)sender).Checked)
            {
                textBox7.Enabled = false;
            }
            else
            {
                textBox7.Enabled = true;
            }

        }

        // get set methods begin here
        public string DocumentDirectoryName
        {
            get { return textBox1.Text; }
            set { textBox1.Text = value; }
        }
        public string ServerName
        {
            get { return textBox2.Text; }
            set { textBox2.Text = value; }
        }
        public string MainDatabaseName
        {
            get { return textBox3.Text; }
            set { textBox3.Text = value; }
        }
        public string VaultDatabaseName
        {
            get { return textBox6.Text; }
            set { textBox6.Text = value; }
        }
        public string SQLAuthType
        {
            get { return comboBox1.Text; }
            set { comboBox1.Text = value; }
        }
        public string SQLUserName
        {
            get { return textBox4.Text; }
            set { textBox4.Text = value; }
        }
        public string SQLPassword
        {
            get { return textBox5.Text; }
            set { textBox5.Text = value; }
        }
        public string BatchLoadVersion
        {
            get { return textBox7.Text; }
            set { textBox7.Text = value; }
        }
        public Boolean GetDeleteAllCheckBox()
        {
            return checkBox2.Checked;
        }
        public Boolean GetBinaryRadio()
        {
            return radioButton2.Checked;
        }
        public Boolean GetBase64Radio()
        {
            return radioButton1.Checked;
        }
        public Boolean GetCompressedCheckBox()
        {
            return checkBox1.Checked;
        }
        public string ProgressMessage
        {
            get { return label9.Text; }
            set { label9.Text = value; }
        }
        private List<string> GetTestUserInputsAsList()
        {
            List<string> userInputs = new List<string>();
            userInputs.Add(DocumentDirectoryName);
            userInputs.Add(ServerName);
            userInputs.Add(MainDatabaseName);
            userInputs.Add(VaultDatabaseName);
            userInputs.Add(SQLAuthType);
            userInputs.Add(SQLUserName);
            userInputs.Add(SQLPassword);
            userInputs.Add(BatchLoadVersion);

            return userInputs;
        }
        public void SetToolTips()
        {
            toolTip1.SetToolTip(checkBox2, "Caution - This option will delete all previously migrated documents via this program");
            toolTip2.SetToolTip(textBox7, "This will be the keyword to find out different versions of document uploads");
        }

        // This function will open up the template excel file
        private void button2_Click(object sender, EventArgs e)
        {            
            try
            {
                string file = @"..\..\tempFile\emptyMetaDateDocument.xlsx";
                Process.Start(file);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            
        }
    }
}
