﻿using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections;
using System.Collections.Generic;

using kcptun_gui.Model;
using kcptun_gui.Controller;
using kcptun_gui.Properties;
using kcptun_gui.View.Forms;
using kcptun_gui.Common;

namespace kcptun_gui.View
{
    public class MenuViewController
    {
        private MainController controller;

        private NotifyIcon _notifyIcon;
        private ContextMenu contextMenu1;

        private MenuItem enableItem;
        private MenuItem ServersItem;
        private MenuItem seperatorItem;
        private MenuItem startItem;
        private MenuItem stopItem;
        private MenuItem restartItem;
        private MenuItem killAllItem;
        private MenuItem autoStartupItem;
        private MenuItem verboseLoggingItem;
        private MenuItem checkGUIUpdateAtStartupItem;
        private MenuItem checkKcpTunUpdateAtStartupItem;
        private MenuItem AboutItems;
        private MenuItem aboutSeperatorItem;

        private EidtServersForm editServersForm;
        private AboutForm aboutForm;
        private CustomKCPTunForm customKcptunForm;
        private StatisticsForm statisticsForm;

        private bool _firstRun;

        public MenuViewController(MainController controller)
        {
            this.controller = controller;

            Configuration config = controller.ConfigController.GetCurrentConfiguration();
            I18N.SetLang(config.language);

            I18N.LangChanged += onLanguageChanged;

            LoadMenu();

            controller.KCPTunnelController.Started += OnKCPTunnelStarted;
            controller.KCPTunnelController.Stoped += OnKCPTunnelStoped;
            controller.ConfigController.ConfigChanged += OnConfigChanged;
            controller.UpdateChecker.CheckUpdateCompleted += OnCheckUpdateCompleted;

            _notifyIcon = new NotifyIcon();
            UpdateTrayIcon();
            _notifyIcon.Visible = true;
            _notifyIcon.ContextMenu = contextMenu1;

            LoadCurrentConfiguration();

            _firstRun = config.isDefault;
            if (_firstRun)
            {
                ShowEditServicesForm();
            }

            if (config.check_gui_update)
            {
                controller.UpdateChecker.CheckUpdateForGUI(5000);
            }
            if (config.check_kcptun_update)
            {
                controller.UpdateChecker.CheckUpdateForKCPTun(5000);
            }
        }

        private void onLanguageChanged(object sender, EventArgs e)
        {
            UpdateMenuItemsText(contextMenu1.MenuItems);
        }

        private void LoadCurrentConfiguration()
        {
            Configuration config = controller.ConfigController.GetConfigurationCopy();
            UpdateServersMenu();
            UpdateAboutMenu();
            enableItem.Checked = config.enabled;
            autoStartupItem.Checked = AutoStartup.Check();
            verboseLoggingItem.Checked = config.verbose;
            checkGUIUpdateAtStartupItem.Checked = config.check_gui_update;
            checkKcpTunUpdateAtStartupItem.Checked = config.check_kcptun_update;
        }

        private void UpdateMenuItemsText(Menu.MenuItemCollection items)
        {
            foreach(MenuItem item in items)
            {
                if (item is MyMenuItem)
                {
                    ((MyMenuItem)item).onLangChanged();
                }
                if (item.MenuItems.Count > 0)
                    UpdateMenuItemsText(item.MenuItems);
            }
        }

        private MenuItem CreateMenuItem(string text, EventHandler click)
        {
            return new MyMenuItem(text, click);
        }

        private MenuItem CreateMenuGroup(string text, MenuItem[] items)
        {
            return new MyMenuItem(text, items);
        }

        private void LoadMenu()
        {
            this.contextMenu1 = new ContextMenu(new MenuItem[] {
                this.enableItem = CreateMenuItem("Enable", new EventHandler(this.OnEnableItemClick)),
                this.ServersItem = CreateMenuGroup("Servers", new MenuItem[] {
                    this.seperatorItem = new MenuItem("-"),
                    CreateMenuItem("Edit Servers...", new EventHandler(this.OnConfigItemClick)),
                    new MenuItem("-"),
                    CreateMenuGroup("Start/Stop/Restart kcptun client...", new MenuItem[] {
                        this.startItem = CreateMenuItem("Start", new EventHandler(this.OnStartItemClick)),
                        this.restartItem = CreateMenuItem("Restart", new EventHandler(this.OnRestartItemClick)),
                        this.stopItem = CreateMenuItem("Stop", new EventHandler(this.OnStopItemClick)),
                        new MenuItem("-"),
                        this.killAllItem = CreateMenuItem("Kill all kcptun clients", new EventHandler(this.OnKillAllItemClick)),
                    }),
                }),
                new MenuItem("-"),
                this.autoStartupItem = CreateMenuItem("Start on Boot", new EventHandler(this.OnAutoStartupItemClick)),
                new MenuItem("-"),
                CreateMenuGroup("More...", new MenuItem[] {
                    this.verboseLoggingItem = CreateMenuItem("Turn on KCP Log", new EventHandler(this.OnVerboseLoggingItemClick)),
                    CreateMenuItem("Custome KCPTun", new EventHandler(this.OnCustomeKCPTunItemClick)),
                }),
                CreateMenuItem("Traffic Statistics", new EventHandler(this.OnStatisticsItemClick)),
                CreateMenuItem("Show Logs...", new EventHandler(this.OnShowLogItemClick)),
                CreateMenuGroup("Update...", new MenuItem[] {
                    CreateMenuItem("Check GUI updates...", new EventHandler(this.OnCheckGUIUpdatesItemClick)),
                    CreateMenuItem("Check kcptun updates...", new EventHandler(this.OnCheckKcpTunUpdatesItemClick)),
                    new MenuItem("-"),
                    this.checkGUIUpdateAtStartupItem = CreateMenuItem("Check GUI updates at startup", new EventHandler(this.OnCheckGUIUpdateAtStartupItemClick)),
                    this.checkKcpTunUpdateAtStartupItem = CreateMenuItem("Check kcptun updates at startup", new EventHandler(this.OnCheckKcpTunUpdateAtStartup)),
                }),
                this.AboutItems = CreateMenuGroup("About...", new MenuItem[] {
                    this.aboutSeperatorItem = new MenuItem("-"),
                    CreateMenuItem("About Form", new EventHandler(this.OnAboutItemClick)),
                }),
                new MenuItem("-"),
                CreateMenuItem("Quit", new EventHandler(this.OnQuitItemClick))
            });

            this.startItem.Enabled = true;
            this.restartItem.Enabled = false;
            this.stopItem.Enabled = false;
        }

        private void UpdateServersMenu()
        {
            var items = ServersItem.MenuItems;
            while (items[0] != seperatorItem)
            {
                items.RemoveAt(0);
            }
            Configuration configuration = controller.ConfigController.GetConfigurationCopy();
            for (int i = 0; i < configuration.servers.Count; i++)
            {
                Server server = configuration.servers[i];
                MenuItem item = new MenuItem(server.FriendlyName());
                item.Tag = i;
                item.Click += OnServerItemClick;
                if (i == configuration.index)
                    item.Checked = true;
                items.Add(i, item);
            }
        }

        private void UpdateAboutMenu()
        {
            var items = AboutItems.MenuItems;
            while (items[0] != aboutSeperatorItem)
            {
                items.RemoveAt(0);
            }

            Configuration config = controller.ConfigController.GetConfigurationCopy();
            I18N.SetLang(config.language);

            IList<I18N.Lang> langlist = I18N.GetLangList();
            for (int i = 0; i < langlist.Count; i++)
            {
                I18N.Lang lang = langlist[i];
                MenuItem item = new MenuItem(I18N.GetString(lang.fullname));
                item.Tag = lang;
                item.Click += OnLanguageItemClick;
                if (lang == I18N.Current)
                    item.Checked = true;
                items.Add(i, item);
            }
        }

        private void UpdateTrayIcon()
        {
            Bitmap bitmap = Resources.logo_small;
            Bitmap iconCopy = new Bitmap(bitmap);

            Configuration config = controller.ConfigController.GetCurrentConfiguration();
            Server server = config.GetCurrentServer();
            string runningInfo = "";

            if (controller.IsKcptunRunning)
            {
                /*do nothing*/
            }
            else if (!config.enabled)
            {
                iconCopy = Utils.ToGray(iconCopy);
            }
            else
            {
                iconCopy = Utils.ToBlue(iconCopy);
                runningInfo = I18N.GetString("Running...") + "\n";
            }

            string text = $"kcptun {UpdateChecker.GUI_VERSION}\n" + runningInfo + I18N.GetString("Local:") +" (" + server.localaddr + ")\n"+ I18N.GetString("Remote:") + " " + server.FriendlyName() + "";

            _notifyIcon.Icon = Icon.FromHandle(iconCopy.GetHicon());
            _notifyIcon.Text = text.Substring(0, Math.Min(63, text.Length));

            iconCopy.Dispose();
        }

        private void ShowBalloonTip(string title, string content, ToolTipIcon icon, int timeout)
        {
            _notifyIcon.BalloonTipTitle = title;
            _notifyIcon.BalloonTipText = content;
            _notifyIcon.BalloonTipIcon = icon;
            _notifyIcon.ShowBalloonTip(timeout);
        }

        private void ShowFirstTimeBalloon()
        {
            ShowBalloonTip(I18N.GetString("kcptun is here"),
                I18N.GetString("You can turn on/off kcptun in the context menu."),
                ToolTipIcon.Info, 0);
        }

        private void ShowAboutForm()
        {
            if (aboutForm != null)
            {
                aboutForm.Activate();
            }
            else
            {
                aboutForm = new AboutForm(controller);
                aboutForm.Show();
                aboutForm.FormClosed += OnAboutFormClosed;
            }
        }

        private void OnAboutFormClosed(object sender, FormClosedEventArgs e)
        {
            aboutForm = null;
        }

        private void ShowCustomKCPTunForm()
        {
            if (customKcptunForm != null)
            {
                customKcptunForm.Activate();
            }
            else
            {
                customKcptunForm = new CustomKCPTunForm(controller);
                customKcptunForm.Show();
                customKcptunForm.FormClosed += OnCustomKCPTunFormClosed;
            }
        }

        private void OnCustomKCPTunFormClosed(object sender, FormClosedEventArgs e)
        {
            customKcptunForm = null;
        }

        private void ShowEditServicesForm()
        {
            if (editServersForm != null)
            {
                editServersForm.Activate();
            }
            else
            {
                editServersForm = new EidtServersForm(controller);
                editServersForm.Show();
                editServersForm.FormClosed += OnEditServersFormClosed;
            }
        }

        private void OnEditServersFormClosed(object sender, FormClosedEventArgs e)
        {
            editServersForm = null;
            if (_firstRun)
            {
                _firstRun = false;
                ShowFirstTimeBalloon();
            }
        }

        private void ShowStatisticsForm()
        {
            if (statisticsForm != null)
            {
                statisticsForm.Activate();
            }
            else
            {
                statisticsForm = new StatisticsForm(controller);
                statisticsForm.Show();
                statisticsForm.FormClosed += OnStatisticsFormClosed;
            }
        }

        private void OnStatisticsFormClosed(object sender, FormClosedEventArgs e)
        {
            statisticsForm = null;
        }

        private void OnEnableItemClick(object sender, EventArgs e)
        {
            controller.ConfigController.ToggleEnable(!enableItem.Checked);
        }

        private void OnServerItemClick(object sender, EventArgs e)
        {
            MenuItem item = (MenuItem)sender;
            controller.ConfigController.SelectServerIndex((int)item.Tag);
        }

        private void OnConfigItemClick(object sender, EventArgs e)
        {
            ShowEditServicesForm();
        }

        private void OnStartItemClick(object sender, EventArgs e)
        {
            try
            {
                controller.KCPTunnelController.Server = controller.ConfigController.GetCurrentServer();
                controller.KCPTunnelController.Start();
            }
            catch (Exception ex)
            {
                Logging.LogUsefulException(ex);
                MessageBox.Show(ex.Message, I18N.GetString("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnStopItemClick(object sender, EventArgs e)
        {
            try
            {
                controller.KCPTunnelController.Stop();
            }
            catch (Exception ex)
            {
                Logging.LogUsefulException(ex);
                MessageBox.Show(ex.Message, I18N.GetString("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnRestartItemClick(object sender, EventArgs e)
        {
            try
            {
                if (controller.KCPTunnelController.IsRunning)
                    controller.KCPTunnelController.Stop();
                controller.KCPTunnelController.Server = controller.ConfigController.GetCurrentServer();
                controller.KCPTunnelController.Start();
            }
            catch (Exception ex)
            {
                Logging.LogUsefulException(ex);
                MessageBox.Show(ex.Message, I18N.GetString("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnKillAllItemClick(object sender, EventArgs e)
        {
            try
            {
                controller.KCPTunnelController.KillAll();
            }
            catch (Exception ex)
            {
                Logging.LogUsefulException(ex);
                MessageBox.Show(ex.Message, I18N.GetString("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnAutoStartupItemClick(object sender, EventArgs e)
        {
            autoStartupItem.Checked = !autoStartupItem.Checked;
            if (!AutoStartup.Set(autoStartupItem.Checked))
            {
                MessageBox.Show(I18N.GetString("Failed to update registry"));
            }
        }

        private void OnVerboseLoggingItemClick(object sender, EventArgs e)
        {
            controller.ConfigController.ToggleVerboseLogging(!verboseLoggingItem.Checked);
        }

        private void OnCustomeKCPTunItemClick(object sender, EventArgs e)
        {
            ShowCustomKCPTunForm();
        }

        private void OnStatisticsItemClick(object sender, EventArgs e)
        {
            ShowStatisticsForm();
        }

        private void OnShowLogItemClick(object sender, EventArgs e)
        {
            new LogForm(controller).Show();
        }

        private void OnAboutItemClick(object sender, EventArgs e)
        {
            ShowAboutForm();
        }

        private void OnQuitItemClick(object sender, EventArgs e)
        {
            controller.Stop();
            _notifyIcon.Visible = false;
            Application.Exit();
        }

        private void OnConfigChanged(object sender, EventArgs e)
        {
            LoadCurrentConfiguration();
            UpdateTrayIcon();
        }

        private void OnKCPTunnelStarted(object sender, EventArgs e)
        {
            UpdateTrayIcon();
            this.startItem.Enabled = false;
            this.restartItem.Enabled = true;
            this.stopItem.Enabled = true;
        }

        private void OnKCPTunnelStoped(object sender, EventArgs e)
        {
            UpdateTrayIcon();
            this.startItem.Enabled = true;
            this.restartItem.Enabled = false;
            this.stopItem.Enabled = false;
        }

        private void OnCheckUpdateCompleted(object sender, UpdateChecker.CheckUpdateEventArgs e)
        {
            try
            {
                if (e.ReleaseList.Count == 0)
                {
                    Logging.Debug($"No {e.Name} update is available");
                    if (e.UserState != null)
                    {
                        if (e.Name == UpdateChecker.GUI_PROJECT_NAME)
                        {
                            ShowBalloonTip("",
                                I18N.GetString("GUI is up to date"),
                                ToolTipIcon.Info, 3000);
                        }
                        else if (e.Name == UpdateChecker.KCPTUN_PROJECT_NAME)
                        {
                            ShowBalloonTip("",
                                I18N.GetString("kcptun is up to date"),
                                ToolTipIcon.Info, 3000);
                        }
                    }
                }
                else
                {
                    UpdateChecker.Release release = e.ReleaseList[0];
                    if (e.Name == UpdateChecker.GUI_PROJECT_NAME)
                    {
                        ShowBalloonTip("",
                            string.Format(I18N.GetString("New GUI version {0} is available"), release.version),
                            ToolTipIcon.Info, 3000);
                    }
                    else if (e.Name == UpdateChecker.KCPTUN_PROJECT_NAME)
                    {
                        ShowBalloonTip("",
                            string.Format(I18N.GetString("New kcptun version {0} is available"), release.version),
                            ToolTipIcon.Info, 3000);
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.LogUsefulException(ex);
            }
        }

        private void OnCheckGUIUpdateAtStartupItemClick(object sender, EventArgs e)
        {
            controller.ConfigController.ToggleCheckGUIUpdate(!checkGUIUpdateAtStartupItem.Checked);
        }

        private void OnCheckKcpTunUpdateAtStartup(object sender, EventArgs e)
        {
            controller.ConfigController.ToggleCheckKCPTunUpdate(!checkKcpTunUpdateAtStartupItem.Checked);
        }

        private void OnCheckGUIUpdatesItemClick(object sender, EventArgs e)
        {
            controller.UpdateChecker.CheckUpdateForGUI(100, this);
        }

        private void OnCheckKcpTunUpdatesItemClick(object sender, EventArgs e)
        {
            controller.UpdateChecker.CheckUpdateForKCPTun(100, this);
        }

        private void OnLanguageItemClick(object sender, EventArgs e)
        {
            MenuItem item = (MenuItem)sender;
            I18N.Lang lang = (I18N.Lang)item.Tag;
            I18N.SetLang(lang.name);
            controller.ConfigController.SelectLanguage(lang);
        }


        class MyMenuItem : MenuItem
        {
            private string _originalText;

            public MyMenuItem(string text, EventHandler onClick)
                : base(I18N.GetString(text), onClick)
            {
                _originalText = text;
            }

            public MyMenuItem(string text, MenuItem[] items)
                : base(I18N.GetString(text), items)
            {
                _originalText = text;
            }

            public void onLangChanged()
            {
                this.Text = I18N.GetString(_originalText);
            }
        }

    }
}
