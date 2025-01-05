using System;
using System.Reactive;
using Avalonia.Controls;
using ChatAAC.Helpers;
using ChatAAC.Lang;
using ReactiveUI;

namespace ChatAAC.ViewModels
{
    public class AboutViewModel : ViewModelBase
    {
        // ----------------------------
        // Localized Text Properties
        // ----------------------------

        public string AboutWindowTitle => Resources.AboutWindowTitle;
        public string AboutTitle => Resources.AboutTitle;
        public string AboutVersion => Resources.AboutVersion;
        public string AboutDescription => Resources.AboutDescription;
        public string AboutAuthor => Resources.AboutAuthor;
        public string AboutContact => Resources.AboutContact;
        
        public string AboutArasaacTitle => Resources.AboutArasaacTitle;
        public string AboutArasaacDescription => Resources.AboutArasaacDescription;
        
        public string AboutLicensesTitle => Resources.AboutLicensesTitle;
        public string AboutLicensesDescription => Resources.AboutLicensesDescription;
        
        public string AboutCloseButton => Resources.AboutCloseButton;
        public string AboutCloseButtonAutomation => Resources.AboutCloseButtonAutomation;
        public ReactiveCommand<Window, Unit> CloseWindowCommand { get; private set; }

        public AboutViewModel()
        {
            CloseWindowCommand   = ReactiveCommand.Create<Window>(OnCloseWindow);
        }

        private void OnCloseWindow(Window? window)
        {
            window?.Close();
        }
        
        /// <summary>
        /// Call this to update properties after switching the culture in <see cref="Resources"/>.
        /// </summary>
        public void RefreshLocalizedTexts()
        {
            // Notifies the UI that all properties may have changed,
            // causing Avalonia to re-bind them.
            this.RaisePropertyChanged(string.Empty);
        }


    }
}