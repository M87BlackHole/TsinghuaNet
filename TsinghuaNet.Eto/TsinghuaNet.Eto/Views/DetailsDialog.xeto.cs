﻿using Eto.Forms;
using Eto.Serialization.Xaml;
using TsinghuaNet.Eto.ViewModels;

namespace TsinghuaNet.Eto.Views
{
    public class DetailsDialog : Dialog
    {
        private DetailViewModel Model;

        public DetailsDialog()
        {
            XamlReader.Load(this);
            DataContext = Model = new DetailViewModel();
        }
    }
}
