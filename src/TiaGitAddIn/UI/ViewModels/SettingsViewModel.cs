using System;
using Microsoft.Win32;
using TiaGitAddIn.Configuration;
using TiaGitAddIn.Models;
using TiaGitAddIn.UI;

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

        public SettingsViewModel(
            IConfigurationService configurationService,
            string repositoryRoot,
            IUiDispatcher? uiDispatcher = null)
            : base(uiDispatcher)
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
                if (SetProperty(gitExecutablePath, value ?? string.Empty, updated => gitExecutablePath = updated))
                {
                    Validate();
                }
            }
        }

        public string RepositoryPath
        {
            get => repositoryPath;
            set => SetProperty(repositoryPath, value ?? string.Empty, updated => repositoryPath = updated);
        }

        public string DefaultRemote
        {
            get => defaultRemote;
            set => SetProperty(defaultRemote, value ?? string.Empty, updated => defaultRemote = updated);
        }

        public int MaxLogEntries
        {
            get => maxLogEntries;
            set => SetProperty(maxLogEntries, value, updated => maxLogEntries = updated);
        }

        public string ValidationMessage
        {
            get => validationMessage;
            private set => SetProperty(validationMessage, value ?? string.Empty, updated => validationMessage = updated);
        }

        public RelayCommand BrowseGitExeCommand { get; }
        public RelayCommand SaveCommand { get; }

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
            GitConfiguration config = new()
            {
                GitExecutablePath = string.IsNullOrWhiteSpace(GitExecutablePath) ? null : GitExecutablePath,
                RepositoryPath = RepositoryPath,
                DefaultRemote = DefaultRemote,
                MaxLogEntries = MaxLogEntries,
                Version = 1
            };

            try
            {
                configurationService.Save(repositoryRoot, config);
                ValidationMessage = "Settings saved successfully.";
            }
            catch (Exception ex)
            {
                ValidationMessage = $"Failed to save settings: {ex.Message}";
            }

            InvokeOnUI(() => SaveCommand.RaiseCanExecuteChanged());
        }

        private bool CanSave() => string.IsNullOrEmpty(ValidationMessage) || ValidationMessage == "Settings saved successfully.";

        private void BrowseGitExe()
        {
            OpenFileDialog dialog = new()
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
            InvokeOnUI(() => SaveCommand.RaiseCanExecuteChanged());
        }

    }
}
