﻿using ModManagerCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static ModManagerWPF.MainWindow;

namespace ModManagerWPF
{
    /// <summary>
    /// Interaction logic for AboutMod.xaml
    /// </summary>
    public partial class AboutMod : Window
    {
        public AboutMod(ModData mod)
        {
			DataContext = mod;
			InitializeComponent();
			Title += " " + mod.Name;
			TitleText.Text = mod.Name + " " + mod.Version;
			AuthorText.Text = mod.Author != null ? "By " + mod.Author : null;
			DescBx.Text = mod.Description;
        }
    }
}
