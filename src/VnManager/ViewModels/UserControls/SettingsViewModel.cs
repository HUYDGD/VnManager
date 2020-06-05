﻿using Stylet;
using StyletIoC;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using VndbSharp.Models.Common;
using System.Xml;
using System.Xml.Serialization;
using VnManager.Models.Settings;
using System.IO;
using VnManager.Helpers;
using System.Linq;
using System.Reflection;
using System.Resources;

namespace VnManager.ViewModels.UserControls
{
    public class SettingsViewModel :Screen
    {
        private static readonly ResourceManager Rm = new ResourceManager("VnManager.Strings.Resources", Assembly.GetExecutingAssembly());
        private readonly string configFile = Path.Combine(App.ConfigDirPath, @"config\config.xml");
        public string Theme { get; set; } = "DarkTheme";
        public bool NsfwEnabled { get; set; }
        public bool NsfwContentSavedVisible { get; set; }

        #region SpoilerList
        public Collection<string> SpoilerList { get;} = new Collection<string>(new string[] { Rm.GetString("None"), Rm.GetString("Minor"), Rm.GetString("Major") });
        public string SpoilerString { get; set; }
        public int SpoilerIndex { get; set; } = 0;
        #endregion

        public static SettingsViewModel Instance { get; private set; }


        public SettingsViewModel()
        {
            if(Instance != null)
            {
                Instance = this;
            }            
        }

        public void SaveUserSettings(bool useEncryption = false)
        {
            Enum.TryParse(SpoilerString, out SpoilerLevel spoiler);
            UserSettingsVndb vndb = new UserSettingsVndb
            {
                Spoiler = spoiler
            };
            UserSettings settings = new UserSettings
            {
                ColorTheme = Theme,
                IsNsfwEnabled = NsfwEnabled,
                IsVisibleSavedNsfwContent = NsfwContentSavedVisible,
                SettingsVndb = vndb,
                EncryptionEnabled = useEncryption
            };

            try
            {
                var serializer = new XmlSerializer(typeof(UserSettings));
                using (var writer = new StreamWriter(configFile))
                {
                    serializer.Serialize(writer, settings);
                }
                App.UserSettings = settings;
            }
            catch (Exception ex)
            {
                App.Logger.Error(ex, "Couldn't write to config file");
                throw;
            }
        }

        public void LoadUserSettings()
        {
            if(!File.Exists(configFile))
            {
                CreateDefaultConfig();
            }
            try
            {
                
                using (var fs = new FileStream(configFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    //var serializer = new XmlSerializer(typeof(UserSettings));
                    XmlSerializer serializer = XmlSerializer.FromTypes(new[] { typeof(UserSettings) })[0]; //TODO: Check for memory leaks if this run multiple times
                    bool isValid = ValidateXml.IsValidXml(configFile);
                    App.UserSettings = isValid == true ? (UserSettings)serializer.Deserialize(fs) : null;
                }
                
            }
            catch (Exception ex)
            {
                App.Logger.Error(ex, "Couldn't load config file");
                throw;
            }
        }

        public static void LoadUserSettingsStatic()
        {
            if(Instance== null)
            {
                Instance = new SettingsViewModel();
            }
            Instance.LoadUserSettings();
        }


        private void CreateDefaultConfig()
        {
            var settings = new UserSettings();
            try
            {
                var serializer = new XmlSerializer(typeof(UserSettings));
                using (var writer = new StreamWriter(configFile))
                {
                    serializer.Serialize(writer, settings);
                }
            }
            catch (Exception ex)
            {
                App.Logger.Error(ex, "Couldn't write to config file");
                throw;
            }
        }

        private void DeleteNsfwImages()
        {
            //Use CheckWriteAccess to see if you can delete from the images

            throw new NotImplementedException();
        }




    }

}
