﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using AdysTech.CredentialManager;
using LiteDB;
using Stylet;
using VnManager.Extensions;
using VnManager.Helpers;
using VnManager.Models.Db;
using VnManager.Models.Db.User;
using VnManager.Models.Db.Vndb.Main;
using VnManager.ViewModels.UserControls.MainPage.Vndb;

namespace VnManager.ViewModels.UserControls.MainPage.NoSource
{
    public class NoSourceMainViewModel: Screen
    {
        public BitmapSource CoverImage { get; set; }
        public BitmapSource GameIcon { get; set; }
        public string Title { get; set; }
        public Visibility IsStartButtonVisible { get; set; }
        public string LastPlayed { get; set; }
        public string PlayTime { get; set; }
        
        private UserDataGames _selectedGame;
        private List<Process> _processList = new List<Process>();
        private bool _isGameRunning = false;
        private readonly Stopwatch _gameStopwatch = new Stopwatch();
        private readonly IWindowManager _windowManager;

        public NoSourceMainViewModel(IWindowManager windowManager)
        {
            _windowManager = windowManager;
        }

        protected override void OnViewLoaded()
        {
            LoadMainData();
        }

        
        public override Task<bool> CanCloseAsync()
        {
            if (_isGameRunning)
            {
                _windowManager.ShowMessageBox(App.ResMan.GetString("ClosingDisabledGameMessage"), App.ResMan.GetString("ClosingDisabledGameTitle"), MessageBoxButton.OK,
                    MessageBoxImage.Exclamation);
                return Task.FromResult(false);

            }
            return base.CanCloseAsync();
        }

        public void CloseClick()
        {
            this.RequestClose();
        }

        internal void SetSelectedGame(UserDataGames game)
        {
            _selectedGame = game;
        }
        
        private void LoadMainData()
        {
            Title = _selectedGame.Title;
            GameIcon = ImageHelper.CreateIcon(!string.IsNullOrEmpty(_selectedGame.IconPath) ? _selectedGame.IconPath : _selectedGame.ExePath);
            LastPlayed = TimeDateChanger.GetHumanDate(_selectedGame.LastPlayed);
            PlayTime = TimeDateChanger.GetHumanTime(_selectedGame.PlayTime);
            var coverName = $"{Path.Combine(App.AssetDirPath, @"sources\noSource\images\cover\")}{_selectedGame.Id}.png";
            CoverImage = File.Exists(coverName) ? ImageHelper.CreateBitmapFromPath(coverName) : ImageHelper.CreateEmptyBitmapImage();
            IsStartButtonVisible = Visibility.Visible;
        }

        public void StartVn()
        {
            if (_isGameRunning)
            {
                return;
            }

            if (_selectedGame?.ExePath != null && File.Exists(_selectedGame.ExePath))
            {
                var filePath = _selectedGame.ExePath;
                Directory.SetCurrentDirectory(Path.GetDirectoryName(filePath));
                var process = new Process { StartInfo = { FileName = filePath, Arguments = _selectedGame.Arguments }, EnableRaisingEvents = true };
                _processList.Add(process);
                process.Exited += MainOrChildProcessExited;
                
                process.Start();
                _isGameRunning = true;
                _gameStopwatch.Start();
                IsStartButtonVisible = Visibility.Collapsed;
                _processList.AddRange(process.GetChildProcesses());
            }
        }

        public void StopVn()
        {
            const int maxWaitTime = 30000; //30 seconds
            foreach (var process in _processList)
            {
                process.CloseMainWindow();
                process.WaitForExit(maxWaitTime);
            }

            _processList = _processList.Where(x => x.HasExited == false).ToList();
            foreach (var process in _processList)
            {
                process.Kill(true);
            }

            _gameStopwatch.Stop();

            var cred = CredentialManager.GetCredentials(App.CredDb);
            if (cred == null || cred.UserName.Length < 1)
            {
                return;
            }

            using (var db = new LiteDatabase($"{App.GetDbStringWithoutPass}{cred.Password}"))
            {
                var dbUserData = db.GetCollection<UserDataGames>(DbUserData.UserData_Games.ToString());
                var gameEntry = dbUserData.Query().Where(x => x.Id == _selectedGame.Id).FirstOrDefault();
                gameEntry.LastPlayed = DateTime.UtcNow;
                gameEntry.PlayTime = _gameStopwatch.Elapsed;
                dbUserData.Update(gameEntry);
            }
            _isGameRunning = false;
            _gameStopwatch.Reset();
            IsStartButtonVisible = Visibility.Visible;
            _processList.Clear();
            
        }
        
        private void MainOrChildProcessExited(object sender, EventArgs e)
        {
            var process = (Process)sender;
            if (process == null)
            {
                return;
            }
            var children = process.GetChildProcesses().ToArray();
            if (children.Length > 0)
            {
                foreach (var childProcess in children)
                {
                    childProcess.EnableRaisingEvents = true;
                    childProcess.Exited += MainOrChildProcessExited;
                }
                _processList.AddRange(children);
                _processList = _processList.Where(x => x.HasExited == false).ToList();
            }
            else
            {
                _gameStopwatch.Stop();
                var cred = CredentialManager.GetCredentials(App.CredDb);
                if (cred == null || cred.UserName.Length < 1)
                {
                    return;
                }

                using (var db = new LiteDatabase($"{App.GetDbStringWithoutPass}{cred.Password}"))
                {
                    var dbUserData = db.GetCollection<UserDataGames>(DbUserData.UserData_Games.ToString());
                    var gameEntry = dbUserData.Query().Where(x => x.Id == _selectedGame.Id).FirstOrDefault();
                    gameEntry.LastPlayed = DateTime.UtcNow;
                    gameEntry.PlayTime = _gameStopwatch.Elapsed;
                    dbUserData.Update(gameEntry);
                }
                _gameStopwatch.Reset();
                _isGameRunning = false;
                IsStartButtonVisible = Visibility.Visible;
            }
        }
    }
}
