using System;
using System.Windows.Input;
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
        private string siemensCompareToolPath = string.Empty;
        private string nodeExecutablePath = string.Empty;
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
            BrowseSiemensCompareToolCommand = new RelayCommand(_ => BrowseSiemensCompareTool());
            BrowseNodeExeCommand = new RelayCommand(_ => BrowseNodeExe());
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

        public string SiemensCompareToolPath
        {
            get => siemensCompareToolPath;
            set => SetProperty(siemensCompareToolPath, value ?? string.Empty, updated => siemensCompareToolPath = updated);
        }

        public string NodeExecutablePath
        {
            get => nodeExecutablePath;
            set => SetProperty(nodeExecutablePath, value ?? string.Empty, updated => nodeExecutablePath = updated);
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

        public ICommand BrowseGitExeCommand { get; }
        public ICommand BrowseSiemensCompareToolCommand { get; }
        public ICommand BrowseNodeExeCommand { get; }
        public ICommand SaveCommand { get; }

        private void LoadSettings()
        {
            var config = configurationService.Load(repositoryRoot);
            gitExecutablePath = config.GitExecutablePath ?? string.Empty;
            siemensCompareToolPath = config.SiemensCompareToolPath ?? string.Empty;
            nodeExecutablePath = config.NodeExecutablePath ?? string.Empty;
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
                SiemensCompareToolPath = string.IsNullOrWhiteSpace(SiemensCompareToolPath) ? null : SiemensCompareToolPath,
                NodeExecutablePath = string.IsNullOrWhiteSpace(NodeExecutablePath) ? null : NodeExecutablePath,
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

        private void BrowseSiemensCompareTool()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "SIMATIC Automation Compare Tool|Compare.exe;CompareTool.exe|All Files (*.*)|*.*",
                Title = "Select SIMATIC Automation Compare Tool"
            };

            if (dialog.ShowDialog() == true)
            {
                SiemensCompareToolPath = System.IO.Path.GetDirectoryName(dialog.FileName) ?? dialog.FileName;
            }
        }

        private void BrowseNodeExe()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Node Executable (node.exe)|node.exe|All Files (*.*)|*.*",
                Title = "Select node.exe"
            };

            if (dialog.ShowDialog() == true)
            {
                NodeExecutablePath = dialog.FileName;
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
            InvokeOnUI(() => ((RelayCommand)SaveCommand).RaiseCanExecuteChanged());
        }

    }
}
