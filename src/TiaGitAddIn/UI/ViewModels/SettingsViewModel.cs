using System;
using System.Windows.Input;
using Microsoft.Win32;
using TiaGitAddIn.Configuration;
using TiaGitAddIn.Models;

namespace TiaGitAddIn.UI.ViewModels
{
    public sealed class SettingsViewModel : ViewModelBase
    {
        private readonly IConfigurationService configurationService;
        private readonly string repositoryRoot;
        private string gitExecutablePath = string.Empty;
        private string repositoryPath = string.Empty;
        private string defaultRemote = "origin";
        private int maxLogEntries = 200;
        private string validationMessage = string.Empty;

        public SettingsViewModel(IConfigurationService configurationService, string repositoryRoot)
        {
            this.configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            this.repositoryRoot = repositoryRoot;

            BrowseGitExeCommand = new RelayCommand(_ => BrowseGitExe());
            SaveCommand = new RelayCommand(_ => Save(), _ => CanSave());

            LoadSettings();
        }

        public string GitExecutablePath
        {
            get => gitExecutablePath;
            set
            {
                if (SetProperty(ref gitExecutablePath, value ?? string.Empty))
                {
                    Validate();
                }
            }
        }

        public string RepositoryPath
        {
            get => repositoryPath;
            set => SetProperty(ref repositoryPath, value ?? string.Empty);
        }

        public string DefaultRemote
        {
            get => defaultRemote;
            set => SetProperty(ref defaultRemote, value ?? string.Empty);
        }

        public int MaxLogEntries
        {
            get => maxLogEntries;
            set => SetProperty(ref maxLogEntries, value);
        }

        public string ValidationMessage
        {
            get => validationMessage;
            private set => SetProperty(ref validationMessage, value ?? string.Empty);
        }

        public ICommand BrowseGitExeCommand { get; }
        public ICommand SaveCommand { get; }

        private void LoadSettings()
        {
            var config = configurationService.Load(repositoryRoot);
            gitExecutablePath = config.GitExecutablePath ?? string.Empty;
            repositoryPath = config.RepositoryPath ?? repositoryRoot;
            defaultRemote = config.DefaultRemote ?? "origin";
            maxLogEntries = config.MaxLogEntries;

            OnPropertyChanged(string.Empty);
            Validate();
        }

        private void Save()
        {
            var config = new GitConfiguration
            {
                GitExecutablePath = string.IsNullOrWhiteSpace(GitExecutablePath) ? null : GitExecutablePath,
                RepositoryPath = RepositoryPath,
                DefaultRemote = DefaultRemote,
                MaxLogEntries = MaxLogEntries,
                Version = 1
            };

            configurationService.Save(repositoryRoot, config);
            ValidationMessage = "Settings saved successfully.";
        }

        private bool CanSave() => string.IsNullOrEmpty(ValidationMessage) || ValidationMessage == "Settings saved successfully.";

        private void BrowseGitExe()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Git Executable (git.exe)|git.exe|All Files (*.*)|*.*",
                Title = "Select git.exe"
            };

            if (dialog.ShowDialog() == true)
            {
                GitExecutablePath = dialog.FileName;
            }
        }

        private void Validate()
        {
            if (!string.IsNullOrWhiteSpace(GitExecutablePath))
            {
                var result = PathValidator.ValidateGitExecutablePath(GitExecutablePath);
                ValidationMessage = result.IsValid ? string.Empty : result.ErrorMessage ?? "Invalid path";
            }
            else
            {
                ValidationMessage = string.Empty;
            }
            ((RelayCommand)SaveCommand).RaiseCanExecuteChanged();
        }
    }
}
