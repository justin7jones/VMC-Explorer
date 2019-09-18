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
using System.Text.RegularExpressions;

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
            System.Diagnostics.Process.Start("http://automatevi.com/blog/2019/09/13/generating-an-api-token-in-vmware-vmc/");
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
            listBox1.Items.Add("Attempting to retrieve Bearer token... ");

            var authentication_RESPONSE = client.Post(authentication_REQUEST);

            listBox1.Items.Add("Authentication Reponse Code: " + authentication_RESPONSE.StatusCode);

            JObject response_object = JObject.Parse(authentication_RESPONSE.Content);

            Global._AccessToken = response_object.SelectToken("$.access_token").ToString();

            listBox1.Items.Add("Access Token Retrived.  Good for 120 minutes.");

            // If responsecode was valid, show connected

            if (authentication_RESPONSE.StatusCode.ToString() == "OK")

            {
                Connected_VMC_Startup();
            }
            else
            {
                listBox1.Items.Add("TROUBLESHOOTING: Authentication Response Content: " + authentication_RESPONSE.Content);
            }
        }

        private void Connected_VMC_Startup()
        {
            // This method executes when connected to VMC Successfully

            label4.Text = "Connected to VMC API";
            label4.BackColor = Color.LightGreen;
            timer1.Enabled = true;

            // Enable Org Selection
            groupBox3.Enabled = true;

            // Get Orgs and populate

            var client = new RestClient("https://vmc.vmware.com/vmc/api");
            var request_segment = "orgs";
            var authentication_REQUEST = new RestRequest(request_segment);

            authentication_REQUEST.AddParameter("Authorization", "Bearer " + Global._AccessToken, ParameterType.HttpHeader);

            listBox1.Items.Add("Invoking REST Call to Retrieve Org List: " + client.BuildUri(authentication_REQUEST));

            var authentication_RESPONSE = client.Get(authentication_REQUEST);


            /******* Parse response for orgs *******/

            JArray response_object = JArray.Parse(authentication_RESPONSE.Content);

            
            var temp_org_names = response_object.SelectTokens("$.[*].display_name").ToList();
            var temp_org_GUIDs = response_object.SelectTokens("$.[*].id").ToList();
            
            // Load up all the orgs
            int i = 0;
            comboBox1.Items.Clear();


            foreach (var temp_org_name in temp_org_names)
                {
                Global.org_name_list.Add(temp_org_name.Value<string>());
                comboBox1.Items.Add(Global.org_name_list[i]);
                i++;
                }

            // Now load up the corresponding org GUIDs
            i = 0;
            foreach (var temp_org_GUID in temp_org_GUIDs)
            {
                Global.org_GUID_list.Add(temp_org_GUID.Value<string>());
                i++;
            }

            // Select the first org
            comboBox1.Text = comboBox1.Items[0].ToString();

            /******* Parse response for SDDCs ******/
            

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
            if (Global.AUTH_EXPIRE_TIMER <= 0) timer1.Enabled = false;

        }

        private void ComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

            // Only run this code if there is at least one Org

            if (comboBox1.Items[0].ToString() != "<not connected>")
            {
                // Selected Org changed, query for SDDCs


                // Get the selected index

                int selected_index = comboBox1.SelectedIndex;

                String selected_org_guid = Global.org_GUID_list[selected_index];

                // Set the read only textbox to contain the Org GUID
                textBox2.Text = selected_org_guid;
                // Build query for SDDCs for this org


                var client = new RestClient("https://vmc.vmware.com/vmc/api/orgs/" + selected_org_guid);
                var request_segment = "sddcs";
                var authentication_REQUEST = new RestRequest(request_segment);

                listBox1.Items.Add("INVOKING REST COMMAND TO GET SDDCs: " + client.BuildUri(authentication_REQUEST).ToString());

                authentication_REQUEST.AddParameter("Authorization", "Bearer " + Global._AccessToken, ParameterType.HttpHeader);

                var authentication_RESPONSE = client.Get(authentication_REQUEST);

                if ((authentication_RESPONSE.StatusCode.ToString()) == "OK")
                {



                    /******* Parse response for SDDCs *******/
                    try
                    {
                        JArray response_object = JArray.Parse(authentication_RESPONSE.Content);

                        var temp_sddc_names = response_object.SelectTokens("$.[*].name").ToList();
                        var temp_sddc_GUIDs = response_object.SelectTokens("$.[*].id").ToList();

                        // Load up all the SDDCs
                        int i = 0;

                        // Clear the combo box
                        comboBox2.Items.Clear();
                        // Load up the Global variable with the SDDC Names
                        foreach (var temp_sddc_name in temp_sddc_names)
                        {
                            Global.sddc_name_list.Add(temp_sddc_name.Value<string>());
                            comboBox2.Items.Add(Global.sddc_name_list[i]);
                            i++;
                        }
                        // Load up the Global variable with SDDC GUIDs


                        foreach (var temp_sddc_GUID in temp_sddc_GUIDs)
                        {
                            Global.sddc_GUID_list.Add(temp_sddc_GUID.Value<string>());
                        }

                        // Select the first item
                        comboBox2.SelectedIndex = 0;
                    }
                    catch
                    {

      
                    }

                } // end if for status of GET ORGS == OK

                if ((authentication_RESPONSE.StatusCode.ToString()) != "OK")
                {

                    comboBox2.Items.Clear();
                    comboBox2.Items.Add("<not connected>");
                    MessageBox.Show("Error attempting to retrieve list of SDDCs. If you have access to multiple ORGs, please select the ORG that corresponds to the API Key provided.", "Error Retrieving SDDC List", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    listBox1.Items.Add("TROUBLESHOOTING: Full REST return from getting SDDCs: " + authentication_RESPONSE.Content.ToString());

                }

            } // end if no org selected



        }

        private void ComboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox1.Items[0].ToString() != "<not connected>")
            {
                int selected_sddc_index = comboBox2.SelectedIndex;
                textBox3.Text = Global.sddc_GUID_list[selected_sddc_index].ToString();

                // load SDDC info
                load_sddc_info();

            } // end if connected only
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            load_sddc_info();
        }

        public void load_sddc_info()
        {
            // Get SDDC Info

            var client = new RestClient("https://vmc.vmware.com/vmc/api/orgs/" + textBox2.Text + "/sddcs");
            var request_segment = textBox3.Text;
            var authentication_REQUEST = new RestRequest(request_segment);

            listBox1.Items.Add("INVOKING REST COMMAND TO GET SDDC Details: " + client.BuildUri(authentication_REQUEST).ToString());

            authentication_REQUEST.AddParameter("Authorization", "Bearer " + Global._AccessToken, ParameterType.HttpHeader);

            var authentication_RESPONSE = client.Get(authentication_REQUEST);

            // Pretty up the returned JSON
            var pretty_json = JsonConvert.DeserializeObject(authentication_RESPONSE.Content);

            richTextBox1.Text = pretty_json.ToString();

            // Get this NSX-T Reverse Proxy API Endpoint

            JObject response_object = JObject.Parse(authentication_RESPONSE.Content);

            // Parse the SDDC JSON for needed info
            textBox6.Text   = comboBox2.Text;
            textBox7.Text   = Global.sddc_GUID_list[comboBox2.SelectedIndex];
            textBox8.Text   = response_object.SelectToken("$.version").ToString();
            textBox9.Text   = response_object.SelectToken("$.resource_config.vc_url").ToString();
            textBox10.Text  = response_object.SelectToken("$.resource_config.psc_url").ToString();
            textBox11.Text  = response_object.SelectToken("$.resource_config.nsx_mgr_url").ToString();
            var nsx_api_endpoint = response_object.SelectToken("$.resource_config.nsx_api_public_endpoint_url").ToString();
            textBox4.Text = nsx_api_endpoint;

            // Now load the firewall sections into the checkbox list

            // NSX Endpoint
            client = new RestClient(textBox4.Text);
            request_segment = "/policy/api/v1/infra/domains/cgw/communication-maps";
            authentication_REQUEST = new RestRequest(request_segment);
            authentication_REQUEST.AddParameter("Authorization", "Bearer " + Global._AccessToken, ParameterType.HttpHeader);


            listBox1.Items.Add("INVOKING REST COMMAND TO GET SDDC Details: " + client.BuildUri(authentication_REQUEST).ToString());

            authentication_RESPONSE = client.Get(authentication_REQUEST);




            // Parse for the Section Names
            var communication_entry_result = JObject.Parse(authentication_RESPONSE.Content);

            var temp_comm_map_names     = communication_entry_result.SelectTokens("$.results[*].display_name").ToList();
            var temp_com_map_ids        = communication_entry_result.SelectTokens("$.results[*].id").ToList();


            // Load up all the checkboxlist
            int i = 0;
            checkedListBox1.Items.Clear();
            Global.comm_map_names.Clear();
            Global.comm_map_ids.Clear();

            foreach (var comm_name in temp_comm_map_names)
            {
                Global.comm_map_names.Add(comm_name.Value<string>());
                checkedListBox1.Items.Add(Global.comm_map_names[i]);
                i++;
            }

            // Now load up the corresponding comm map GUIDs
            i = 0;
            foreach (var comm_id in temp_com_map_ids)
            {
                Global.comm_map_ids.Add(comm_id.Value<string>());
                i++;
            }

        }

       
        private void Button3_Click(object sender, EventArgs e)
        {


            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox5.Text = folderBrowserDialog1.SelectedPath;
                button4.Enabled = true;

            }
        }

        private void CheckBox3_CheckedChanged(object sender, EventArgs e)
        {
            // toggle all checkboxes appropriately

            if (checkBox3.Checked)
            {
                // Select all
                checkBox4.Checked = true;
                checkBox5.Checked = true;
                checkBox6.Checked = true;
                checkBox7.Checked = true;


                // Check everything in the checkboxlist
                for (int i=0; i<checkedListBox1.Items.Count; i++)
                {
                    checkedListBox1.SetItemChecked(i, true);
                }

            }
            else
            {
                // deselect all
                checkBox4.Checked = false;
                checkBox5.Checked = false;
                checkBox6.Checked = false;
                checkBox7.Checked = false;

                // UnCheck everything in the checkboxlist
                for (int i = 0; i < checkedListBox1.Items.Count; i++)
                {
                    checkedListBox1.SetItemChecked(i, false);
                }

            }
        }

        private void Button4_Click(object sender, EventArgs e)
        {
            create_backup_package();
        }

        public void create_backup_package()
        {
            // Build out folder structure
            DateTime today = DateTime.Now;


            String backup_path = textBox5.Text + "\\vmc_fw_backup_" + today.ToString("yyyy") + "_" + today.ToString("MM") + "_" + today.ToString("dd") + "_" + today.ToString("HH") + "_" + today.ToString("mm") + "_" + today.ToString("ss");
            Directory.CreateDirectory(backup_path);

            // Set up Backup API call
            var client = new RestClient(textBox4.Text);
            var request_segment = "not_set";
            IRestRequest authentication_REQUEST;
            IRestResponse authentication_RESPONSE;

            String target_directory         = "not_set";
            String target_filename_fullpath = "not set";
            int files_count = 0;
            // Backup management groups
            if (checkBox4.Checked)
            {
                request_segment = "/policy/api/v1/infra/domains/mgw/groups";

                authentication_REQUEST = new RestRequest(request_segment);
                authentication_REQUEST.AddParameter("Authorization", "Bearer " + Global._AccessToken, ParameterType.HttpHeader);
                listBox1.Items.Add("INVOKING REST COMMAND TO GET MGW Groups: " + client.BuildUri(authentication_REQUEST).ToString());

                authentication_RESPONSE = client.Get(authentication_REQUEST);

                target_directory = backup_path + "\\Groups";
                target_filename_fullpath = target_directory + "\\Management_Groups.json";
                Directory.CreateDirectory(target_directory);

                File.WriteAllText((target_filename_fullpath), authentication_RESPONSE.Content.ToString());
                files_count++;
            }
            // Backup workload groups
            if (checkBox5.Checked)
            {
                request_segment = "/policy/api/v1/infra/domains/cgw/groups";

                authentication_REQUEST = new RestRequest(request_segment);
                authentication_REQUEST.AddParameter("Authorization", "Bearer " + Global._AccessToken, ParameterType.HttpHeader);
                listBox1.Items.Add("INVOKING REST COMMAND TO GET CGW Groups: " + client.BuildUri(authentication_REQUEST).ToString());
                
                authentication_RESPONSE = client.Get(authentication_REQUEST);

                target_directory = backup_path + "\\Groups";
                target_filename_fullpath = target_directory + "\\Workload_Groups.json";
                Directory.CreateDirectory(target_directory);

                File.WriteAllText((target_filename_fullpath), authentication_RESPONSE.Content.ToString());
                files_count++;
            }
            // Backup Management Groups

            if (checkBox4.Checked)
            {
                request_segment = "/policy/api/v1/infra/domains/mgw/groups";

                authentication_REQUEST = new RestRequest(request_segment);
                authentication_REQUEST.AddParameter("Authorization", "Bearer " + Global._AccessToken, ParameterType.HttpHeader);
                listBox1.Items.Add("INVOKING REST COMMAND TO GET CGW Groups: " + client.BuildUri(authentication_REQUEST).ToString());

                authentication_RESPONSE = client.Get(authentication_REQUEST);

                target_directory = backup_path + "\\Groups";
                target_filename_fullpath = target_directory + "\\Workload_Groups.json";
                Directory.CreateDirectory(target_directory);

                File.WriteAllText((target_filename_fullpath), authentication_RESPONSE.Content.ToString());
                files_count++;
            }

            // Compute Gateway FW Rules backup
            if (checkBox6.Checked)
            {
                request_segment = "/policy/api/v1/infra/domains/cgw/groups";

                authentication_REQUEST = new RestRequest(request_segment);
                authentication_REQUEST.AddParameter("Authorization", "Bearer " + Global._AccessToken, ParameterType.HttpHeader);
                listBox1.Items.Add("INVOKING REST COMMAND TO GET CGW Groups: " + client.BuildUri(authentication_REQUEST).ToString());

                authentication_RESPONSE = client.Get(authentication_REQUEST);

                target_directory = backup_path + "\\Groups";
                target_filename_fullpath = target_directory + "\\Workload_Groups.json";
                Directory.CreateDirectory(target_directory);

                File.WriteAllText((target_filename_fullpath), authentication_RESPONSE.Content.ToString());
                files_count++;
            }


            // Backups complete, invoke messagebox

            MessageBox.Show("Backup Complete.  Backups have been exported to " + backup_path + "\n\n Total Number of Files Exported: " + files_count.ToString(),"VMC Firewall Backup Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);

        }

        private void Button5_Click(object sender, EventArgs e)
        {
            // Write the SDDC Contents to file.
            var client = new RestClient("https://vmc.vmware.com/vmc/api/orgs/" + textBox2.Text + "/sddcs");
            var request_segment = textBox3.Text;
            var authentication_REQUEST = new RestRequest(request_segment);

            listBox1.Items.Add("INVOKING REST COMMAND TO GET SDDC Details: " + client.BuildUri(authentication_REQUEST).ToString());

            authentication_REQUEST.AddParameter("Authorization", "Bearer " + Global._AccessToken, ParameterType.HttpHeader);

            var authentication_RESPONSE = client.Get(authentication_REQUEST);

            // Pretty up the returned JSON
            var pretty_json = JsonConvert.DeserializeObject(authentication_RESPONSE.Content);

            richTextBox1.Text = pretty_json.ToString();



        }

        private void Button5_Click_1(object sender, EventArgs e)
        {

        }
    } // end Class

    static class Global
    {
        // This class is used to store global items needed across multiple forms 
        private static string _refreshToken     = "";
        private static string _accessToken      = "";
        private static string _SDDC_guid        = "";
        private static string _ORG_guid         = "";
        private static  int auth_expire_timer   = 1800;
        public static List<string> org_name_list   = new List<string>();
        public static List<string> sddc_name_list  = new List<string>();
        public static List<string> org_GUID_list   = new List<string>();
        public static List<string> sddc_GUID_list  = new List<string>();

        public static List<string> comm_map_names   = new List<string>();
        public static List<string> comm_map_ids     = new List<string>();

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

    } // end class Global


}
