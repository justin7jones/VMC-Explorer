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

            var access_token = response_object.SelectToken("$.access_token").ToString();

            listBox1.Items.Add("Access Token: " + access_token);
        }
    }

    static class Global
    {
        // This class is used to store global items needed across multiple forms 
        private static string _refreshToken = "";
        private static string _accessToken = "";
        private static string _SDDC_guid = "";
        private static string _ORG_guid = "";

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

    } // end class
}
