using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using RestSharp;
using RestSharp.Authenticators;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Security;
using System.Security.AccessControl;
using System.IO;

namespace VMC_Explorer
{

   
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void LinkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://www.automatevi.com");
        }

        private void CheckBox1_CheckedChanged(object sender, EventArgs e)
        {

            if (checkBox1.Checked)
                textBox1.PasswordChar = '\0';
            else
                textBox1.PasswordChar = Convert.ToChar("*");
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Initialize Combo boxes
            comboBox1.SelectedItem = comboBox1.Items[0];
            comboBox2.SelectedItem = comboBox2.Items[0];

            // check if they saved their API key and if so, load it

            if (File.Exists(Global.fullPath))
            {
                // In this case, the apiKey file exists
               checkBox2.Checked = true;

                // Decrypting API Key
                listBox1.Items.Add("Found Encrypted API Key in My Docs. Folder. Loading...");

                Global._RefreshToken = File.ReadAllText(Global.fullPath);
                textBox1.Text = Global._RefreshToken;

                
                

            }
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            Global._RefreshToken = textBox1.Text;
            var client = new RestClient("https://console.cloud.vmware.com/csp/gateway/am/api");
            var request_segment = "auth/api-tokens/authorize?refresh_token=" + Global._RefreshToken;
            var authentication_REQUEST = new RestRequest(request_segment);



            listBox1.Items.Clear();
            listBox1.Items.Add("RefreshToken = " + Global._RefreshToken);
            listBox1.Items.Add("Attempting to connect to: " + client.BuildUri(authentication_REQUEST));

            var authentication_RESPONSE = client.Post(authentication_REQUEST);

            listBox1.Items.Add("Reponse Code: " + authentication_RESPONSE.StatusCode);
            listBox1.Items.Add("Reponse Body:" + authentication_RESPONSE.Content);

            JObject response_object = JObject.Parse(authentication_RESPONSE.Content);

            Global._AccessToken = response_object.SelectToken("$.access_token").ToString();

            listBox1.Items.Add("Access Token: " + Global._AccessToken);

            // If responsecode was valid, show connected

            if (authentication_RESPONSE.StatusCode.ToString() == "OK")

            {
                Connected_VMC_Startup();
            }
        }

        private void Connected_VMC_Startup()
        {
            // This method executes when connected to VMC Successfully

            label4.Text = "Connected to VMC API";
            label4.BackColor = Color.LightGreen;
            timer1.Enabled = true;

            // Get Orgs and populate

            var client = new RestClient("https://vmc.vmware.com/vmc/api");
            var request_segment = "orgs";
            var authentication_REQUEST = new RestRequest(request_segment);

            authentication_REQUEST.AddParameter("Authorization", "Bearer " + Global._AccessToken, ParameterType.HttpHeader);

            var authentication_RESPONSE = client.Get(authentication_REQUEST);


            // Parse response for orgs

            JArray response_object = JArray.Parse(authentication_RESPONSE.Content);

            
            var temp_orgs = response_object.SelectTokens("$.[*].display_name").ToList();
            
            // Load up all the orgs
            int i = 0;
            comboBox1.Items.Clear();

            foreach (var temp_org in temp_orgs)
                {
                Global.org_name_array[i] = temp_orgs[i].ToString();
                MessageBox.Show("Array Item: " + temp_orgs.ElementAt(i));
                comboBox1.Items.Add(Global.org_name_array[i]);
                i++;
                }

            

        }

        private void CheckBox2_CheckedChanged(object sender, EventArgs e)
        {
            // Only overwrite the key IF the file \MyDocs\vmc_api_key.secured file does not exist

            if (!File.Exists(Global.fullPath))
            {

            // This code saves the API Key in an encrypted file to their My Docs folder
            var apiKey = textBox1.Text;

            // write the file
            File.WriteAllText((Global.fullPath), apiKey);

            // Encrypt the file
            File.Encrypt(Global.fullPath);
            }

        }

        private void LinkLabel3_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            // This method clears a saved API Key

            // Delete the saved API Key File
            File.Delete(Global.fullPath);
            checkBox2.Checked = false;

            MessageBox.Show("The API Key file has been deleted but will still be in Recycle Bin.  Empty to fully remove.  Other users will still not be able to read the contents.", "API Key: Read to Fully Remove", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            
        }

        private void LinkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://www.automatevi.com");
        }

        private void Timer1_Tick(object sender, EventArgs e)
        {
            Global.AUTH_EXPIRE_TIMER--;
            label5.Text = "Auth Expires In: " + Global.AUTH_EXPIRE_TIMER.ToString() + "sec";
        }
    }

    static class Global
    {
        // This class is used to store global items needed across multiple forms 
        private static string _refreshToken = "";
        private static string _accessToken = "";
        private static string _SDDC_guid = "";
        private static string _ORG_guid = "";
        private static int auth_expire_timer = 1800;
        public static string[] org_name_array;
        public static string[] sddc_name_array;
        public static string[] org_GUID_array;
        public static string[] sddc_GUID_array;

        // Path where API Key will be stored if saved
        public static string fullPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "vmc_api_key.secured");


        public static string _RefreshToken
        {
            get { return _refreshToken; }
            set { _refreshToken = value; }
        }

        public static string _AccessToken
        {
            get { return _accessToken; }
            set { _accessToken = value; }
        }

        public static string _SDDC_GUID
        {
            get { return _SDDC_guid; }
            set { _SDDC_guid = value; }
        }

        public static string _ORG_GUID
        {
            get { return _ORG_guid; }
            set { _ORG_guid = value; }
        }

        public static int AUTH_EXPIRE_TIMER
        {
            get { return auth_expire_timer; }
            set { auth_expire_timer = value; }
        }

    } // end class
}
