﻿using System.Windows;

namespace NuGet.Dialog.PackageManagerUI
{
    public interface IProgressWindowOpener
    {
        void Show(string title, Window owner);
        void Hide();
        bool IsOpen { get; }
        bool Close();
        void SetCompleted(bool successful);
        void AddMessage(MessageLevel level, string message);
        void ClearMessages();
        void ShowProgress(string operation, int percentComplete);
    }
}