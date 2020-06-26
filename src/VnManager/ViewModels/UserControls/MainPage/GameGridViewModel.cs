﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media.Imaging;
using Stylet;
using VnManager.ViewModels.Controls;

namespace VnManager.ViewModels.UserControls.MainPage
{
    public class GameGridViewModel: Screen
    {
        public BindableCollection<GameCardViewModel> GameCollection { get; set; } = new BindableCollection<GameCardViewModel>();

        public GameGridViewModel()
        {
            var rand = new Random();
            for (int i = 0; i < 30; i++)
            {
                var bol = rand.Next() > (Int32.MaxValue / 2);
                var bi = new BitmapImage();
                bi.BeginInit();
                bi.UriSource = new Uri("https://upload.wikimedia.org/wikipedia/en/c/c2/Tron_Legacy_poster.jpg"); /*new Uri("https://s2.vndb.org/cv/23/23223.jpg");*/
                bi.EndInit();
                var card = new GameCardViewModel()
                {
                    CoverImage = bi,
                    Title = $"Title {i}",
                    LastPlayedString = $"Last Played: {i}",
                    TotalTimeString = $"Total Time: {i}",
                    IsNsfwDisabled = bol
                };
                GameCollection.Add(card);
            }
        }
    }
}
