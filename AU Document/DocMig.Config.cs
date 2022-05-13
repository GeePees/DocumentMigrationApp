using System.Collections.Generic;

namespace AU_Document
{
    public class DataBaseConfig
    {
        private string connString;
        private string timeOut = "90";

        public string GetDatabaseConfig(string dbname, List<string> userInputs)
        {
            SetDatabaseConfig(dbname, userInputs);

            return connString;
        }

        // set connection string
        public void SetDatabaseConfig(string dbname, List<string> userInputs)
        {
            string databasename = "";

            if (dbname == "Main")
            {
                databasename = userInputs[2];
            }
            else if (dbname == "Vault")
            {
                databasename = userInputs[3];
            }

            connString = @"data source=" + userInputs[1] + "; initial catalog=" + databasename;
            if (userInputs[4] == "SQL Server Auth")
            {
                connString = connString + "; user id=" + userInputs[5] + "; password=" + userInputs[6];
            }
            else if (userInputs[4] == "Windows Auth")
            {
                connString += "; Integrated Security=SSPI";
            }
            connString = connString + "; Connection Timeout=" + timeOut + "; MultipleActiveResultSets=True;";
        }
    }
}
