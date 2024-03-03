﻿using SAModManager.UI;
using SAModManager.Updater;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Controls.Primitives;
using SAModManager.Configuration;
using SAModManager.Configuration.SADX;
using SAModManager.Ini;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace SAModManager.Controls.SADX
{
    /// <summary>
    /// Interaction logic for GameConfig.xaml
    /// </summary>
    public partial class GameConfig : UserControl
    {
        #region Variables
        public GameSettings GameProfile;

		bool suppressEvent = false;
		private static string d3d8to9InstalledDLLName = Path.Combine(App.CurrentGame.gameDirectory, "d3d8.dll");
		private static string d3d8to9StoredDLLName = Path.Combine(App.extLibPath, "d3d8m", "d3d8m.dll");
		private readonly double LowOpacityBtn = 0.7;
		private static string patchesPath = null;
		#endregion

		public GameConfig(ref object gameSettings)
		{
			InitializeComponent();
			GameProfile = (GameSettings)gameSettings;
			UpdateAppLauncherBtn();
            if (Directory.Exists(App.CurrentGame.modDirectory))
            {

                string pathDest = Path.Combine(App.CurrentGame.modDirectory, "Patches.json");
                if (File.Exists(pathDest))
                    patchesPath = pathDest;

                SetPatches();
            }
            Loaded += GameConfig_Loaded;
        }

        #region Internal Functions
        private void GameConfig_Loaded(object sender, RoutedEventArgs e)
        {
            SetupBindings();
            SetPatches();
            SetUp_UpdateD3D9();
            SetTextureFilterList();
            InitMouseList();

            mouseAction.SelectionChanged += mouseAction_SelectionChanged;
            mouseBtnAssign.SelectionChanged += mouseBtnAssign_SelectionChanged;
        }

        //Temporary, TO DO: Implement proper texture filter list
        private void SetTextureFilterSettings()
        {
            if (GameProfile.Graphics.EnableForcedTextureFilter == true)
            {
                comboTextureFilter.SelectedIndex = 0;
                comboTextureFilter.SelectedItem = 0;
            }
            else
            {
                comboTextureFilter.SelectedIndex = 1;
                comboTextureFilter.SelectedItem = 1;
            }
        }

        private void SetTextureFilterList()
        {
            comboTextureFilter.Items.Clear();

            comboTextureFilter.Items.Add(Lang.GetString("CommonStrings.Enabled"));
            comboTextureFilter.Items.Add(Lang.GetString("CommonStrings.Disabled"));

            SetTextureFilterSettings();
        }

        #region Graphics Tab
        private void ResolutionChanged(object sender, RoutedEventArgs e)
        {

            NumericUpDown box = sender as NumericUpDown;

            switch (box.Name)
            {
                case "txtResY":
                    if (chkRatio.IsChecked == true)
                    {
                        double ratio = (4.0 / 3.0);
                        txtResX.Value = Math.Ceiling(txtResY.Value * ratio);
                    }
                    break;
                case "txtCustomResY":
                    if (chkMaintainRatio.IsChecked == true)
                    {
                        double ratio = txtResX.Value / txtResY.Value;
                        txtCustomResX.Value = Math.Ceiling(txtCustomResY.Value * ratio);
                    }
                    break;
            }

            if (!suppressEvent)
                comboDisplay.SelectedIndex = -1;

        }

        private void HorizontalRes_Changed(object sender, RoutedEventArgs e)
        {
            if (!suppressEvent)
                comboDisplay.SelectedIndex = -1;
        }

        private void comboScreen_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
			if (GraphicsManager.Screens.Count > 1)
				GraphicsManager.UpdateResolutionPresets(comboScreen.SelectedIndex);
        }

		private void chkRatio_Click(object sender, RoutedEventArgs e)
		{
			if (chkRatio.IsChecked == true)
			{
				txtResX.IsEnabled = false;
				double resYDecimal = txtResY.Value;
				double roundedValue = Math.Round(resYDecimal * GraphicsManager.GetRatio(GraphicsManager.Ratio.ratio43));
				txtResX.Value = (double)roundedValue;
			}
			else if (!suppressEvent)
			{
				txtResX.IsEnabled = true;
			}
		}

		private void DisplaySize_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			ComboBox box = (ComboBox)sender;

			if (box.SelectedIndex == -1)
				return;

			int index = box.SelectedIndex;

			suppressEvent = true;

			switch (box.Name)
			{
				case "comboDisplay":
					txtResY.Value = GraphicsManager.ResolutionPresets[index].Height;

					if (chkRatio.IsChecked == false)
						txtResX.Value = GraphicsManager.ResolutionPresets[index].Width;
					break;

				case "comboCustomWindow":
					txtCustomResY.Value = GraphicsManager.ResolutionPresets[index].Height;

					if (chkRatio.IsChecked == false)
						txtCustomResX.Value = GraphicsManager.ResolutionPresets[index].Width;
					break;
			}

			suppressEvent = false;
		}

        private void chkMaintainRatio_Click(object sender, RoutedEventArgs e)
        {
            if (chkMaintainRatio.IsChecked == true)
            {
                txtCustomResX.IsEnabled = false;
                double ratio = txtResX.Value / txtResY.Value;
                txtCustomResX.Value = Math.Ceiling(txtCustomResY.Value * ratio);
            }
            else if (!suppressEvent)
            {
                txtCustomResX.IsEnabled = true;
            }
        }

        private void SetUp_UpdateD3D9()
        {
            bool isUpdateAvailable = CheckD3D8to9Update();

            btnUpdateD3D9.Visibility = isUpdateAvailable ? Visibility.Visible : Visibility.Hidden;
            btnUpdateD3D9.IsEnabled = !isUpdateAvailable;
            checkD3D9.IsEnabled = File.Exists(d3d8to9StoredDLLName);
            checkD3D9.IsChecked = File.Exists(d3d8to9InstalledDLLName);
        }

        private void CopyD3D9Dll()
        {
            try
            {
                File.Copy(d3d8to9StoredDLLName, d3d8to9InstalledDLLName, true);
            }
            catch (Exception ex)
            {
                string error = Lang.GetString("MessageWindow.Errors.D3D8Update") + "\n" + ex.Message;
                new MessageWindow(Lang.GetString("MessageWindow.DefaultTitle"), error, MessageWindow.WindowType.IconMessage, MessageWindow.Icons.Error, MessageWindow.Buttons.OK).ShowDialog();
            }
        }

        private bool CheckD3D8to9Update()
        {
            if (!File.Exists(d3d8to9StoredDLLName) || !File.Exists(d3d8to9InstalledDLLName))
                return false;

            try
            {
                long length1 = new FileInfo(d3d8to9InstalledDLLName).Length;
                long length2 = new FileInfo(d3d8to9StoredDLLName).Length;
                if (length1 != length2)
                    return true;
                else
                {
                    byte[] file1 = File.ReadAllBytes(d3d8to9InstalledDLLName);
                    byte[] file2 = File.ReadAllBytes(d3d8to9StoredDLLName);
                    for (int i = 0; i < file1.Length; i++)
                    {
                        if (file1[i] != file2[i])
                            return true;
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                string error = Lang.GetString("MessageWindow.Errors.D3D8UpdateCheck") + "\n" + ex.Message;
                new MessageWindow(Lang.GetString("MessageWindow.DefaultTitle"), error, MessageWindow.WindowType.IconMessage, MessageWindow.Icons.Error).ShowDialog();
                return false;
            }
        }

        private void btnUpdateD3D9_Click(object sender, RoutedEventArgs e)
        {
            string info = Lang.GetString("MessageWindow.Information.D3D8Update");
            var msg = new MessageWindow(Lang.GetString("MessageWindow.DefaultTitle"), Lang.GetString(info), MessageWindow.WindowType.IconMessage, MessageWindow.Icons.Information, MessageWindow.Buttons.YesNo);
            msg.ShowDialog();

            if (msg.isYes)
            {
                CopyD3D9Dll();
                btnUpdateD3D9.IsEnabled = CheckD3D8to9Update();
            }
        }

        private void checkD3D9_Click(object sender, RoutedEventArgs e)
        {
            if (checkD3D9.IsChecked == true)
            {
                CopyD3D9Dll();
            }
            else if (checkD3D9.IsChecked == false && File.Exists(d3d8to9InstalledDLLName))
                File.Delete(d3d8to9InstalledDLLName);

        }
        #endregion

        #region Input Tab
        private void InitMouseList()
        {
            List<string> mouseActionList = new()
            {
                Lang.GetString("GameConfig.Tabs.Input.Group.Input.Group.Vanilla.Start"),
                Lang.GetString("GameConfig.Tabs.Input.Group.Input.Group.Vanilla.Cancel"),
                Lang.GetString("GameConfig.Tabs.Input.Group.Input.Group.Vanilla.Jump"),
                Lang.GetString("GameConfig.Tabs.Input.Group.Input.Group.Vanilla.Action"),
                Lang.GetString("GameConfig.Tabs.Input.Group.Input.Group.Vanilla.Flute"),

            };

            mouseAction.ItemsSource = mouseActionList;

            List<string> mouseBtnAssignList = new()
            {
                Lang.GetString("CommonStrings.None"),
                Lang.GetString("GameConfig.Tabs.Input.Group.Input.Group.Vanilla.Group.MouseKeyboard.LeftMouseBtn"),
                Lang.GetString("GameConfig.Tabs.Input.Group.Input.Group.Vanilla.Group.MouseKeyboard.RightMouseBtn"),
                Lang.GetString("GameConfig.Tabs.Input.Group.Input.Group.Vanilla.Group.MouseKeyboard.MiddleMouseBtn"),
                Lang.GetString("GameConfig.Tabs.Input.Group.Input.Group.Vanilla.Group.MouseKeyboard.OtherMouseBtn"),
                Lang.GetString("GameConfig.Tabs.Input.Group.Input.Group.Vanilla.Group.MouseKeyboard.LeftRightMouseBtn"),
                Lang.GetString("GameConfig.Tabs.Input.Group.Input.Group.Vanilla.Group.MouseKeyboard.RightLeftMouseBtn"),
            };

            mouseBtnAssign.ItemsSource = mouseBtnAssignList;
        }

        private void DisplayInputGroup(int type)
        {
            switch (type)
            {
                default:
                    grpSDLInput.Visibility = Visibility.Visible;
                    grpVanillaInput.Visibility = Visibility.Collapsed;
                    break;
                case 1:
                    grpSDLInput.Visibility = Visibility.Collapsed;
                    grpVanillaInput.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void InputRadioButtonCheck(object sender, RoutedEventArgs e)
        {
            if (grpVanillaInput is null || grpSDLInput is null)
                return;

            if ((bool)radBetterInput.IsChecked)
                DisplayInputGroup(0);

            if ((bool)radVanillaInput.IsChecked)
                DisplayInputGroup(1);
        }

        #region App Launcher
        public static async Task UpdateAppLauncher()
        {
            string fullName = "AppLauncher.7z";
            string destName = App.CurrentGame.gameDirectory;
            string fullPath = Path.Combine(destName, fullName);

            Uri uri = new("https://dcmods.unreliable.network/owncloud/data/PiKeyAr/files/Setup/data/AppLauncher.7z" + "\r\n");
            var DL = new DownloadDialog(uri, "App Launcher", fullName, destName, DownloadDialog.DLType.Update);

            DL.DownloadFailed += (ex) =>
            {
                DL.DisplayDownloadFailedMSG(ex);
            };

            DL.StartDL();

            if (DL.done)
            {
                try
                {
                    await Util.Extract(fullPath, destName, true);

                    string SDL2Game = Path.Combine(App.CurrentGame.gameDirectory, "SDL2.dll");
                    if (File.Exists(SDL2Game))
                    {
                        File.Delete(SDL2Game);
                    }
                }
                catch
                {
                    throw new Exception("Failed to extract AppLauncher.");
                }

                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }

            }

            await Task.Delay(10);
        }

        private async void btnGetAppLauncher_Click(object sender, RoutedEventArgs e)
        {

            string fullName = "AppLauncher.7z";
            string destName = App.CurrentGame.gameDirectory;
            string fullPath = Path.Combine(destName, fullName);

            btnGetAppLauncher.IsEnabled = false;
            btnGetAppLauncher.Opacity = LowOpacityBtn;

            Uri uri = new("https://dcmods.unreliable.network/owncloud/data/PiKeyAr/files/Setup/data/AppLauncher.7z" + "\r\n");
            var DL = new DownloadDialog(uri, "App Launcher", fullName, destName);

            DL.DownloadFailed += (ex) =>
            {
                btnGetAppLauncher.IsEnabled = true;
                btnGetAppLauncher.Opacity = 1;
                DL.DisplayDownloadFailedMSG(ex);
            };

            DL.StartDL();

            if (DL.done)
            {
                try
                {
                    await Util.Extract(fullPath, destName, true);
                    btnOpenAppLauncher.IsEnabled = true;
                    btnOpenAppLauncher.Opacity = 1;
                    btnGetAppLauncher.Opacity = LowOpacityBtn;
                    btnGetAppLauncher.IsEnabled = false;

                }
                catch
                {
                    btnGetAppLauncher.IsEnabled = true;
                    btnGetAppLauncher.Opacity = 1;
                    throw new Exception("Failed to extract AppLauncher.");
                }

                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
            }


            await Task.Delay(10);
        }

        private void btnOpenAppLauncher_Click(object sender, RoutedEventArgs e)
        {
            string fullPath = Path.Combine(App.CurrentGame.gameDirectory, "AppLauncher.exe");

            if (File.Exists(fullPath))
            {
                Process.Start(new ProcessStartInfo { FileName = fullPath, Arguments = "-p1", UseShellExecute = true });
            }
        }

        private void UpdateAppLauncherBtn()
        {
            string fullPath = Path.Combine(App.CurrentGame.gameDirectory, "AppLauncher.exe");

            if (File.Exists(fullPath))
            {
                btnGetAppLauncher.IsEnabled = false;
                btnGetAppLauncher.Opacity = LowOpacityBtn;
            }
            else
            {
                btnOpenAppLauncher.IsEnabled = false;
                btnOpenAppLauncher.Opacity = LowOpacityBtn;
            }
        }
        #endregion
        #endregion

        #region Sound Tab
        private void sliderMusic_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            labelMusicLevel?.SetValue(ContentProperty, $"{(int)sliderMusic.Value}");
        }

        private void sliderVoice_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            labelVoiceLevel?.SetValue(ContentProperty, $"{(int)sliderVoice.Value}");
        }

		private void sliderSFX_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			labelSFXLevel?.SetValue(ContentProperty, $"{(int)sliderSFX.Value}");
		}
		#endregion

        #region Patches Tab
        private PatchesData GetPatchFromView(object sender)
        {
            if (sender is ListViewItem lvItem)
                return lvItem.Content as PatchesData;
            else if (sender is ListView lv)
                return lv.SelectedItem as PatchesData;


            return listPatches.Items[listPatches.SelectedIndex] as PatchesData;
        }

        private void PatchViewItem_MouseEnter(object sender, MouseEventArgs e)
        {

            var patch = GetPatchFromView(sender);

            if (patch is null)
                return;

            PatchAuthor.Text += ": " + patch.Author;
            PatchCategory.Text += ": " + patch.Category;
            PatchDescription.Text += " " + patch.Description;
        }

        private void PatchViewItem_MouseLeave(object sender, MouseEventArgs e)
        {
            PatchAuthor.Text = Lang.GetString("CommonStrings.Author");
            PatchCategory.Text = Lang.GetString("CommonStrings.Category");
            PatchDescription.Text = Lang.GetString("CommonStrings.Description");
        }

        private static List<PatchesData> GetPatches(ref ListView list, GameSettings set)
        {
            list.Items.Clear();

            var patches = PatchesList.Deserialize(patchesPath);

            if (patches is not null)
            {
                var listPatch = patches.Patches;

                foreach (var patch in listPatch)
                {
                    // Convert patch name to the corresponding property name in GamePatches class
                    string propertyName = patch.Name.Replace(" ", ""); // Adjust the naming convention as needed
                    var property = typeof(GamePatches).GetProperty(propertyName);

                    if (property != null)
                    {
                        // Update the IsChecked property based on the GamePatches class
                        patch.IsChecked = (bool)property.GetValue(set.Patches);
                    }

                    string desc = "GamePatches." + patch.Name + "Desc";
                    patch.InternalName = patch.Name;
                    patch.Name = Lang.GetString("GamePatches." + patch.Name);
                    patch.Description = Lang.GetString(desc); //need to use a variable otherwise it fails for some reason
                }

                return listPatch;
            }

            return null;
        }

        public void SetPatches()
        {
            listPatches.Items.Clear();

            List<PatchesData> patches = GetPatches(ref listPatches, GameProfile);

            if (patches is not null)
            {
                foreach (var patch in patches)
                {
                    listPatches.Items.Add(patch);
                }
            }
        }

        private void btnSelectAllPatch_Click(object sender, RoutedEventArgs e)
        {
            foreach (PatchesData patch in listPatches.Items)
            {
                patch.IsChecked = true;
            }
            RefreshPatchesList();
        }

        private void btnDeselectAllPatch_Click(object sender, RoutedEventArgs e)
        {
            foreach (PatchesData patch in listPatches.Items)
            {
                patch.IsChecked = false;
            }
            RefreshPatchesList();

        }

        private void RefreshPatchesList()
        {
            ICollectionView view = CollectionViewSource.GetDefaultView(listPatches.Items);
            view.Refresh();
        }
        #endregion
        #endregion

        public static void UpdateD3D8Paths()
        {
            d3d8to9InstalledDLLName = Path.Combine(App.CurrentGame.gameDirectory, "d3d8.dll");
            d3d8to9StoredDLLName = Path.Combine(App.extLibPath, "d3d8m", "d3d8m.dll");
        }

        public void SavePatches(ref object input)
        {
            GameSettings settings = input as GameSettings;

            if (listPatches is null)
                return;

            foreach (PatchesData patch in listPatches.Items)
            {
                string propertyName = patch.InternalName;
                var propertyInfo = typeof(GamePatches).GetProperty(propertyName);

                if (propertyInfo != null && propertyInfo.CanWrite)
                {
                    propertyInfo.SetValue(settings.Patches, patch.IsChecked);
                }
                else
                {
                    throw new InvalidOperationException($"Property {propertyName} not found or read-only.");
                }
            }
        }

		private void SetItemFromPad(int action)
		{
			switch (action)
			{
				case 0:
					mouseBtnAssign.SelectedIndex = GameProfile.Controller.VanillaMouseStart;
					break;
				case 1:
					mouseBtnAssign.SelectedIndex = GameProfile.Controller.VanillaMouseAttack;
					break;
				case 2:
					mouseBtnAssign.SelectedIndex = GameProfile.Controller.VanillaMouseJump;
					break;
				case 3:
					mouseBtnAssign.SelectedIndex = GameProfile.Controller.VanillaMouseAction;
					break;
				case 4:
					mouseBtnAssign.SelectedIndex = GameProfile.Controller.VanillaMouseFlute;
					break;
			}
		}

		private void SetItemToPad(int value)
		{
			int action = mouseAction.SelectedIndex;
			switch (action)
			{
				case 0:
					GameProfile.Controller.VanillaMouseStart = value;
					break;
				case 1:
					GameProfile.Controller.VanillaMouseAttack = value;
					break;
				case 2:
					GameProfile.Controller.VanillaMouseJump = value;
					break;
				case 3:
					GameProfile.Controller.VanillaMouseAction = value;
					break;
				case 4:
					GameProfile.Controller.VanillaMouseFlute = value;
					break;
			}
		}

		#region Private Functions
		private void SetupBindings()
		{
			// Graphics Bindings

			// Display Options
			comboScreen.ItemsSource = GraphicsManager.Screens;
			comboScreen.DisplayMemberPath = "Key";
			comboScreen.SetBinding(ComboBox.SelectedIndexProperty, new Binding("SelectedScreen")
			{
				Source = GameProfile.Graphics,
				Mode = BindingMode.TwoWay,
			});
			txtResX.MinValue = 0;
			txtResY.MinValue = 0;
			txtResX.SetBinding(NumericUpDown.ValueProperty, new Binding("HorizontalResolution")
			{
				Source = GameProfile.Graphics,
				Mode = BindingMode.TwoWay
			});
			txtResY.SetBinding(NumericUpDown.ValueProperty, new Binding("VerticalResolution")
			{
				Source = GameProfile.Graphics,
				Mode = BindingMode.TwoWay
			});
			chkRatio.SetBinding(CheckBox.IsCheckedProperty, new Binding("Enable43ResolutionRatio")
			{
				Source = GameProfile.Graphics,
				Mode = BindingMode.TwoWay
			});
			comboScreenMode.SetBinding(ComboBox.SelectedIndexProperty, new Binding("ScreenMode")
			{
				Source = GameProfile.Graphics,
				Mode = BindingMode.TwoWay,
				Converter = new ScreenModeConverter(),
			});
			txtCustomResX.MinValue = 0;
			txtCustomResY.MinValue = 0;

			txtCustomResX.SetBinding(NumericUpDown.ValueProperty, new Binding("CustomWindowWidth")
			{
				Source = GameProfile.Graphics,
				Mode = BindingMode.TwoWay
			});
			txtCustomResX.SetBinding(NumericUpDown.IsEnabledProperty, new Binding("ScreenMode")
			{
				Source = GameProfile.Graphics,
				Mode = BindingMode.TwoWay,
				Converter = new CustomWindowEnabledConverter()
			});
			txtCustomResY.SetBinding(NumericUpDown.ValueProperty, new Binding("CustomWindowHeight")
			{
				Source = GameProfile.Graphics,
				Mode = BindingMode.TwoWay
			});
			txtCustomResY.SetBinding(NumericUpDown.IsEnabledProperty, new Binding("ScreenMode")
			{
				Source = GameProfile.Graphics,
				Mode = BindingMode.TwoWay,
				Converter = new CustomWindowEnabledConverter()
			});
			comboCustomWindow.SetBinding(ComboBox.IsEnabledProperty, new Binding("ScreenMode")
			{
				Source = GameProfile.Graphics,
				Mode = BindingMode.TwoWay,
				Converter = new CustomWindowEnabledConverter()
			});
			chkMaintainRatio.SetBinding(CheckBox.IsEnabledProperty, new Binding("ScreenMode")
			{
				Source = GameProfile.Graphics,
				Mode = BindingMode.TwoWay,
				Converter = new CustomWindowEnabledConverter()
			});
			chkMaintainRatio.SetBinding(CheckBox.IsCheckedProperty, new Binding("EnableKeepResolutionRatio")
			{
				Source = GameProfile.Graphics,
				Mode = BindingMode.TwoWay
			});

			// Settings
			chkVSync.SetBinding(CheckBox.IsCheckedProperty, new Binding("EnableVsync")
			{
				Source = GameProfile.Graphics,
				Mode = BindingMode.TwoWay
			});
			chkPause.SetBinding(CheckBox.IsCheckedProperty, new Binding("EnablePauseOnInactive")
			{
				Source = GameProfile.Graphics,
				Mode = BindingMode.TwoWay
			});
			chkShowMouse.SetBinding(CheckBox.IsCheckedProperty, new Binding("ShowMouseInFullscreen")
			{
				Source = GameProfile.Graphics,
				Mode = BindingMode.TwoWay
			});
			chkResizableWin.SetBinding(CheckBox.IsCheckedProperty, new Binding("EnableResizableWindow")
			{
				Source = GameProfile.Graphics,
				Mode = BindingMode.TwoWay
			});

			// Other Visual Settings
			comboFramerate.SetBinding(ComboBox.SelectedIndexProperty, new Binding("GameFrameRate")
			{
				Source = GameProfile.Graphics,
				Mode = BindingMode.TwoWay
			});
			comboDetail.SetBinding(ComboBox.SelectedIndexProperty, new Binding("GameClipLevel")
			{
				Source = GameProfile.Graphics,
				Mode = BindingMode.TwoWay
			});
			comboFog.SetBinding(ComboBox.SelectedIndexProperty, new Binding("GameFogMode")
			{
				Source = GameProfile.Graphics,
				Mode = BindingMode.TwoWay
			});
			comboBGFill.SetBinding(ComboBox.SelectedIndexProperty, new Binding("FillModeBackground")
			{
				Source = GameProfile.Graphics,
				Mode = BindingMode.TwoWay
			});
			comboFMVFill.SetBinding(ComboBox.SelectedIndexProperty, new Binding("FillModeFMV")
			{
				Source = GameProfile.Graphics,
				Mode = BindingMode.TwoWay
			});
			comboTextureFilter.SetBinding(ComboBox.SelectedIndexProperty, new Binding("ModeTextureFiltering")
			{
				Source = GameProfile.Graphics,
				Mode = BindingMode.TwoWay,
			}); ;
			comboUIFilter.SetBinding(ComboBox.SelectedIndexProperty, new Binding("ModeUIFiltering")
			{
				Source = GameProfile.Graphics,
				Mode = BindingMode.TwoWay
			});
			checkMipmapping.SetBinding(CheckBox.IsCheckedProperty, new Binding("EnableForcedMipmapping")
			{
				Source = GameProfile.Graphics,
				Mode = BindingMode.TwoWay
			});
			checkUIScale.SetBinding(CheckBox.IsCheckedProperty, new Binding("EnableUIScaling")
			{
				Source = GameProfile.Graphics,
				Mode = BindingMode.TwoWay
			});

			// Input Settings
			radBetterInput.SetBinding(RadioButton.IsCheckedProperty, new Binding("EnabledInputMod")
			{
				Source = GameProfile.Controller,
				Mode = BindingMode.TwoWay
			});
			inputMouseDragAccel.SetBinding(RadioButton.IsCheckedProperty, new Binding("VanillaMouseUseDrag")
			{
				Source = GameProfile.Controller,
				Mode = BindingMode.TwoWay
			});
			//inputMouseDragHold.IsChecked = (GameSettings.GameConfig.MouseMode == 0) ? true : false;

			// Audio Settings
			checkEnableMusic.SetBinding(CheckBox.IsCheckedProperty, new Binding("EnableGameMusic")
			{
				Source = GameProfile.Sound,
				Mode = BindingMode.TwoWay
			});
			checkEnableSounds.SetBinding(CheckBox.IsCheckedProperty, new Binding("EnableGameSound")
			{
				Source = GameProfile.Sound,
				Mode = BindingMode.TwoWay
			});
			checkBassMusic.SetBinding(CheckBox.IsCheckedProperty, new Binding("EnableBassMusic")
			{
				Source = GameProfile.Sound,
				Mode = BindingMode.TwoWay
			});
			checkBassSFX.SetBinding(CheckBox.IsCheckedProperty, new Binding("EnableBassSFX")
			{
				Source = GameProfile.Sound,
				Mode = BindingMode.TwoWay
			});
			checkEnable3DSound.SetBinding(CheckBox.IsCheckedProperty, new Binding("EnableGameSound3D")
			{
				Source = GameProfile.Sound,
				Mode = BindingMode.TwoWay
			});
			sliderMusic.Minimum = 0;
			sliderMusic.Maximum = 100;
			sliderMusic.SetBinding(ScrollBar.ValueProperty, new Binding("GameMusicVolume")
			{
				Source = GameProfile.Sound,
				Mode = BindingMode.TwoWay
			});
			sliderVoice.Minimum = 0;
			sliderVoice.Maximum = 100;
			sliderVoice.SetBinding(ScrollBar.ValueProperty, new Binding("GameSoundVolume")
			{
				Source = GameProfile.Sound,
				Mode = BindingMode.TwoWay
			});
			sliderSFX.Minimum = 0;
			sliderSFX.Maximum = 100;
			sliderSFX.SetBinding(ScrollBar.ValueProperty, new Binding("SEVolume")
			{
				Source = GameProfile.Sound,
				Mode = BindingMode.TwoWay
			});

			tsVoiceLanguage.SetBinding(ComboBox.SelectedIndexProperty, new Binding("GameVoiceLanguage")
			{ 
				Source = GameProfile.TestSpawn,
				Mode = BindingMode.TwoWay
			});
			tsTextLanguage.SetBinding(ComboBox.SelectedIndexProperty, new Binding("GameTextLanguage")
			{
				Source = GameProfile.TestSpawn,
				Mode = BindingMode.TwoWay
			});
		}
        #endregion

        private void comboTextureFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboTextureFilter.SelectedIndex == 0)
            {
                GameProfile.Graphics.EnableForcedTextureFilter = true;
            }
            else if (comboTextureFilter.SelectedIndex == 1)
            {
                GameProfile.Graphics.EnableForcedTextureFilter = false;
            }
        }

        private void mouseAction_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            SetItemFromPad(comboBox.SelectedIndex);
        }

		private void mouseBtnAssign_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			ComboBox comboBox = sender as ComboBox;
			SetItemToPad(comboBox.SelectedIndex);
		}
    }
}
