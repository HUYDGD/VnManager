﻿using FluentValidation;
using MvvmDialogs;
using MvvmDialogs.FrameworkDialogs.OpenFile;
using Stylet;
using System;
using System.Collections.Generic;
using System.Text;
using VnManager.Helpers;

namespace VnManager.ViewModels.Dialogs
{
    public class AddGameMultiViewModel: Screen
    {
        public BindableCollection<MultiExeGamePaths> GameCollection { get; set; } = new BindableCollection<MultiExeGamePaths>();
        public string ExePath { get; set; }
        public string IconPath { get; set; }
        public string ExeArguments { get; set; }

        private bool _isIconChecked;
        public bool IsIconChecked
        {
            get => _isIconChecked;
            set
            {                
                SetAndNotify(ref _isIconChecked, value);
                if (_isIconChecked == false)
                {
                    IconPath = string.Empty;
                    HideIconError = true;
                    Validate();
                    HideIconError = false;
                }
            }
        }

        private bool _isArgsChecked;
        public bool IsArgsChecked
        {
            get => _isArgsChecked;
            set
            {                
                SetAndNotify(ref _isArgsChecked, value);
                if (_isArgsChecked == false)
                {
                    ExeArguments = string.Empty;
                    HideArgumentsError = true;
                    Validate();
                    HideArgumentsError = false;
                }
            }
        }

        public bool ShowValidationErrors { get; private set; } = true;
        public bool HideArgumentsError { get; private set; } = false;
        public bool HideIconError { get; private set; } = false;

        private readonly IWindowManager _windowManager;
        private readonly IDialogService _dialogService;
        public AddGameMultiViewModel(IWindowManager windowManager, IModelValidator<AddGameMultiViewModel> validator, IDialogService dialogService) : base(validator)
        {
            _windowManager = windowManager;
            _dialogService = dialogService;
        }

        public void Add()
        { 
            var validator = new AddGameMultiViewModelValidator();
            this.Validate();
            bool result = validator.Validate(this).IsValid;
            if (result == true)
            {
                var exe = ExePath;
                var icon = IconPath;
                var args = ExeArguments;

                GameCollection.Add(new MultiExeGamePaths { ExePath = exe, IconPath = icon, ArgumentsString = args });

                ShowValidationErrors = false;//prevent validation errors from showing up after a sucessful Add
                ExePath = string.Empty;
                IconPath = string.Empty;
                ExeArguments = string.Empty;
                Validate();
                ShowValidationErrors = true;
            }
            
            
        }

        public void Remove()
        {
            if(GameCollection.Count > 0)
            {
                GameCollection.RemoveAt(GameCollection.Count -1);
            }
        }
        
        public void BrowseExePath()
        {
            var settings = new OpenFileDialogSettings
            {
                Title = "Browse for Game", 
                DefaultExt = ".exe", 
                Filter = "Applications (*.exe)|*.exe", 
                FileName = "", 
                DereferenceLinks = true, 
                CheckPathExists = true, 
                CheckFileExists = true, 
                ValidateNames = true
            };
            bool? result = _dialogService.ShowOpenFileDialog(this, settings);
            if(result == true)
            {
                ExePath = settings.FileName;
            }
        }

        public void BrowseIconPath()
        {
            var settings = new OpenFileDialogSettings
            {
                Title = "Browse for Game Icon",
                DefaultExt = ".ico",
                Filter = "Icons (*.ico,*.exe)|*.ico;*.exe",
                FileName = "",
                DereferenceLinks = true,
                CheckPathExists = true,
                CheckFileExists = true,
                ValidateNames = true
            };
            bool? result = _dialogService.ShowOpenFileDialog(this, settings);
            if (result == true)
            {
                IconPath = settings.FileName;
            }
        }

        public void Submit()
        {
            
            this.RequestClose(true);
        }

        public void Cancel()
        {
            this.RequestClose(false);
        }

        
    }

    public class AddGameMultiViewModelValidator : AbstractValidator<AddGameMultiViewModel>
    {
        public AddGameMultiViewModelValidator()
        {
            RuleFor(x => x.ExePath).NotEmpty().Unless(x => x.ShowValidationErrors == false).WithMessage("Exe Path cannot be empty");

            RuleFor(x => x.ExePath).Must(ValidateFiles.EndsWithExe).Unless(x => x.ShowValidationErrors == false).When(x => !string.IsNullOrWhiteSpace(x.ExePath) || !string.IsNullOrEmpty(x.ExePath)).WithMessage("Not a valid path to exe");
            RuleFor(x => x.ExePath).Must(ValidateFiles.ValidateExe).Unless(x => x.ShowValidationErrors == false).When(x => !string.IsNullOrWhiteSpace(x.ExePath) || !string.IsNullOrEmpty(x.ExePath)).WithMessage("Not a valid Executable");

            RuleFor(x => x.ExeArguments).Must(ContainsIllegalCharacters).Unless(x => x.ShowValidationErrors == false).When(x => !string.IsNullOrWhiteSpace(x.ExeArguments) || !string.IsNullOrEmpty(x.ExeArguments)).WithMessage("Illegal characters detected");

            RuleFor(x => x.IconPath).Must(ValidateFiles.EndsWithIcoOrExe).Unless(x => x.ShowValidationErrors == false).When(x => !string.IsNullOrWhiteSpace(x.IconPath) || !string.IsNullOrEmpty(x.IconPath)).WithMessage("Not a valid path to icon");

            When(x => x.IsArgsChecked == true && x.ExeArguments == "", () =>
              {
                  RuleFor(x => x.ExeArguments).NotEmpty().Unless(x => x.ShowValidationErrors == false).Unless(x => x.HideArgumentsError == true).WithMessage("Arguments cannot be empty");
              });

            When(x => x.IsIconChecked == true, () =>
            {
                RuleFor(x => x.IconPath).NotEmpty().Unless(x => x.ShowValidationErrors == false).Unless(x => x.HideIconError == true).WithMessage("Icon Path cannot be empty");
            });

        }

        public static bool ContainsIllegalCharacters(string format)
        {
            string allowableLetters = $@"abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890/\-_ {'"'}";

            foreach (char c in format)
            {
                if (!allowableLetters.Contains(c))
                    return false;
            }

            return true;
        }
    }


    public class MultiExeGamePaths
    {
        public string ExePath { get; set; }
        public string IconPath { get; set; }
        public string ArgumentsString { get; set; }
    }
}
