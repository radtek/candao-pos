﻿using CanDao.Pos.UI.Library.ViewModel;

namespace CanDao.Pos.UI.Library.View
{
    /// <summary>
    /// 输入更多信息窗口。
    /// </summary>
    public partial class InputMoreInfoWindow
    {
        private readonly InputMoreInfoWndVm _vm;

        public InputMoreInfoWindow(string title, string info)
        {
            InitializeComponent();
            _vm = new InputMoreInfoWndVm(title, info) { OwnerWindow = this };
            DataContext = _vm;
        }

        public string InputInfo
        {
            get { return _vm.InputInfo; }
        }

    }
}