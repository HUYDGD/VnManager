﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using AdysTech.CredentialManager;
using LiteDB;
using Stylet;
using StyletIoC;
using VndbSharp.Models.Common;
using VnManager.Helpers;
using VnManager.Models.Db;
using VnManager.Models.Db.User;
using VnManager.Models.Db.Vndb.Main;

namespace VnManager.ViewModels.UserControls.MainPage.Vndb
{
    public class VndbScreensViewModel:Screen
    {
        private readonly IContainer _container;
        private readonly IWindowManager _windowManager;

        #region SelectedScreenIndex
        private int _selectedScreenIndex = -1;
        public int SelectedScreenIndex
        {
            get => _selectedScreenIndex;
            set
            {
                SetAndNotify(ref _selectedScreenIndex, value);
                if (_selectedScreenIndex < 0)
                {
                    _selectedScreenIndex = 0;
                }
                LoadLargeScreenshot();
            }
        }
        #endregion

        public ScreenShot MainImage { get; set; }
        
        public BindableCollection<ScreenShot> ScreenshotCollection { get; set; }= new BindableCollection<ScreenShot>();

        public VndbScreensViewModel(IContainer container, IWindowManager windowManager)
        {
            _container = container;
            _windowManager = windowManager;
        }

        protected override void OnViewLoaded()
        {
            BindScreenshotCollection();
            //LoadLargeScreenshot();

        }

        public void ShowInfo()
        {
            var vm = VndbContentViewModel.Instance;
            vm.ActivateVnInfo();
        }

        public static void CloseClick()
        {
            RootViewModel.Instance.ActivateMainClick();
        }


        private static List<ScreenShot> LoadScreenshotList()
        {
            var cred = CredentialManager.GetCredentials(App.CredDb);
            if (cred == null || cred.UserName.Length < 1) return new List<ScreenShot>();
            using var db = new LiteDatabase($"{App.GetDbStringWithoutPass}{cred.Password}");
            var dbUserData = db.GetCollection<VnInfoScreens>(DbVnInfo.VnInfo_Screens.ToString()).Query()
                .Where(x => x.VnId == VndbContentViewModel._vnid).ToEnumerable();
            var scrList = dbUserData.Select(item => new ScreenShot {Uri = item.ImageUri, Rating = item.ImageRating}).ToList();
            return scrList;
        }


        private void LoadLargeScreenshot()
        {
            try
            {
                List<ScreenShot> screenshotList = LoadScreenshotList();
                if (screenshotList.Count <= 0) return;
                string path = $@"{App.AssetDirPath}\sources\vndb\images\screenshots\{VndbContentViewModel._vnid}\{Path.GetFileName(screenshotList[SelectedScreenIndex].Uri.AbsoluteUri)}";
                switch (NsfwHelper.TrueIsNsfw(screenshotList[SelectedScreenIndex].Rating))
                {
                    case true:
                        if (File.Exists($"{path}.aes"))
                        {
                            var imgBytes = File.ReadAllBytes($"{path}.aes");
                            var imgStream = Secure.DecStreamToStream(new MemoryStream(imgBytes));
                            var imgNsfw = ImageHelper.CreateBitmapFromStream(imgStream);
                            MainImage = new ScreenShot{Image = imgNsfw, IsNsfw = NsfwHelper.UserIsNsfw(screenshotList[SelectedScreenIndex].Rating)};
                        }
                        break;
                    case false:
                        var img = ImageHelper.CreateBitmapFromPath(path);
                        MainImage = new ScreenShot{Image = img, IsNsfw = false};
                        break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            
        }

        private void BindScreenshotCollection()
        {
            List<ScreenShot> screenshotList = LoadScreenshotList();
            foreach (var item in screenshotList)
            {
                BitmapSource image;
                if (screenshotList.Count < 1) return;
                string path = $@"{App.AssetDirPath}\sources\vndb\images\screenshots\{VndbContentViewModel._vnid}\thumbs\{Path.GetFileName(item.Uri.AbsoluteUri)}";
                switch (NsfwHelper.TrueIsNsfw(item.Rating))
                {
                    case true when File.Exists($"{path}.aes"):
                        var imgBytes = File.ReadAllBytes($"{path}.aes");
                        var imgStream = Secure.DecStreamToStream(new MemoryStream(imgBytes));
                        image= ImageHelper.CreateBitmapFromStream(imgStream);
                        ScreenshotCollection.Add(new ScreenShot{Image = image, IsNsfw = NsfwHelper.UserIsNsfw(item.Rating)});
                        break;
                    case false when File.Exists(path):
                        image = ImageHelper.CreateBitmapFromPath(path);
                        ScreenshotCollection.Add(new ScreenShot { Image = image, IsNsfw = false });
                        break;
                }
            }
        }

        

    }
}
