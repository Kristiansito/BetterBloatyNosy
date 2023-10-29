using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace BloatyNosy
{
    public partial class IModsPageView : UserControl
    {
        private ModsPageView modsForm = null;

        public IModsPageView(Control ctr)
        {
            modsForm = ctr as ModsPageView;

            InitializeComponent();

            InitializeModsSignature();

            SetStyle();
        }

        private void SetStyle()
        {
            lvMods.BackColor =   Color.FromArgb(25, 25, 25);
            btnBack.Text = "\uE72B";
        }

        private void InitializeModsSignature()
        {
            // Add required columns
            lvMods.Columns.Add("Name");
            lvMods.Columns.Add("Description");
            lvMods.Columns.Add("Developer");



            try
            {
                XDocument doc = XDocument.Load(AppDomain.CurrentDomain.BaseDirectory + "modsApp2.xml");

                foreach (var dm in doc.Descendants("Mod"))
                {
                    ListViewItem item = new ListViewItem(new string[]
                    {
                    dm.Element("id").Value,
                    dm.Element("description").Value,
                    dm.Element("dev").Value,
                    dm.Element("uri").Value,
                    });

                    lvMods.Items.Add(item);

                    lvMods.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
                    lvMods.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
                }

                isFeatureInstalled();
                btnInstall.Enabled = (lvMods.Items.Count > 0);
            }
            catch
            {
                MessageBox.Show("Mods signature file not found.\nPlease re-download and install the signatures.");
                lvMods.Visible = false;
                lnkNoModsSig.Visible = true;
            }
        }

        public void isFeatureInstalled()
        {
            foreach (ListViewItem item in lvMods.Items)
            {
                var feature = item.SubItems[3].Text;

                if (File.Exists(HelperTool.Utils.Data.DataRootDir + feature.Split('/').Last())
                    || File.Exists(HelperTool.Utils.Data.ModsRootDir + feature.Split('/').Last())
                    || File.Exists(AppDomain.CurrentDomain.BaseDirectory + feature.Split('/').Last()))
                {
                    item.ForeColor = Color.Green;
                    item.Checked = true; // Set the checkbox as checked
                }
                else
                {
                    item.ForeColor = Color.White;
                }
            }
            lvMods.ItemCheck += lvMods_ItemCheck;
        }


        private void lvMods_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (lvMods.Items[e.Index].ForeColor == Color.Green)
            {
                e.NewValue = e.CurrentValue; // Cancelar el cambio
            }
        }


        private void btnBack_Click(object sender, EventArgs e)
           => modsForm.SetView(new ModsPageView());

        private async void lnkInstallMods_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                HelperTool.Utils.CreateModsDir();
                using (WebClient client = new WebClient())
                {
                    progress.Visible = true;
                    progress.Value = 0;
                    client.Credentials = CredentialCache.DefaultNetworkCredentials;
                    client.DownloadProgressChanged += Wc_DownloadProgressChanged;
                    Uri uri = new Uri("https://raw.githubusercontent.com/builtbybel/BloatyNosy/main/mods/modsApp2.xml");
                    string filename = System.IO.Path.GetFileName(uri.LocalPath);

                    await client.DownloadFileTaskAsync(uri, AppDomain.CurrentDomain.BaseDirectory + filename);

                    progress.Visible = false;

                    // Update IMods page
                    btnBack.PerformClick();
                    modsForm.lnkExploreMods_LinkClicked(sender, e);
              
                }
            }
            catch (Exception ex)
            { MessageBox.Show(ex.Message); }
        }

        private async void btnInstall_Click(object sender, EventArgs e)
        {
            bool bNeedRestart = false;
            bool nonGreenItemSelected = false; // Flag to track if non-green items are selected

            StringBuilder builder = new StringBuilder();

            foreach (ListViewItem eachItem in lvMods.CheckedItems)
            {
                if (eachItem.ForeColor != Color.Green)
                {
                    nonGreenItemSelected = true; // Set the flag if a non-green item is found

                    List<string> list = new List<string>(eachItem.SubItems[3].Text.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries));

                    foreach (string url in list)
                    {
                        progress.Visible = true;
                        using (WebClient client = new WebClient())
                        {
                            progress.Value = 0;
                            client.Credentials = CredentialCache.DefaultNetworkCredentials;
                            client.DownloadProgressChanged += Wc_DownloadProgressChanged;
                            Uri uri = new Uri(url);
                            string filename = System.IO.Path.GetFileName(uri.LocalPath);
                            string fileExt = System.IO.Path.GetExtension(eachItem.SubItems[3].Text);

                            try
                            {
                                if (fileExt == ".ps1" || fileExt == ".ini")
                                    await client.DownloadFileTaskAsync(uri, HelperTool.Utils.Data.ModsRootDir + filename);
                                else
                                {
                                    HelperTool.Utils.CreateDataDir(); // Create appData folder
                                    await client.DownloadFileTaskAsync(uri, HelperTool.Utils.Data.DataRootDir + filename
                                );
                                }
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(ex.Message, lvMods.FocusedItem.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return;
                            }
                        }
                    }
                    builder.Append("\n- " + eachItem.SubItems[0].Text);

                    if (eachItem.SubItems[3].Text.Contains(".xml"))
                    {
                        builder.Append(" (Restart required.)");
                        bNeedRestart = true;
                    }
                }
            }

            if (!nonGreenItemSelected)
            {
                MessageBox.Show("No feature selected.", "", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            MessageBox.Show("Mods successfully installed:\n" + builder.ToString(), "", MessageBoxButtons.OK, MessageBoxIcon.Information);
            isFeatureInstalled();

            if (bNeedRestart)
            {
                MessageBox.Show("We will restart the app to complete the installation of some features.");
                Application.Restart();
                Environment.Exit(0);
            }

            progress.Visible = false;
        }

        private void Wc_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            progress.Value = e.ProgressPercentage;
            progress.Update();
            // this.Invoke((MethodInvoker)(() => lblStatus.Text = $"{e.ProgressPercentage}%"));
        }

        private void listView_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            //e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(50, 50, 50)), e.Bounds);

            using (Font headerFont = new Font(this.lvMods.Font, FontStyle.Bold))
            {
                TextFormatFlags flags = TextFormatFlags.VerticalCenter | TextFormatFlags.Left;
                TextRenderer.DrawText(e.Graphics, e.Header.Text, headerFont, e.Bounds, Color.White, flags);
            }
        }


        private void listView_DrawItem(object sender, DrawListViewItemEventArgs e)
           => e.DrawDefault = true;

        private void lnkGetModsOnline_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
             => HelperTool.Utils.LaunchUri("https://www.builtbybel.com/blog/about-debloos");

        private void IModsPageView_Load(object sender, EventArgs e)
        {
            lvMods.ColumnWidthChanging += myListView_ColumnWidthChanging;
        }
        private void myListView_ColumnWidthChanging(object sender, ColumnWidthChangingEventArgs e)
        {
            e.Cancel = true; // Esto evita que el usuario cambie el tamaño de la columna en tiempo real.
            e.NewWidth = lvMods.Columns[e.ColumnIndex].Width; // Restablece el ancho de la columna.
        }
    }
}