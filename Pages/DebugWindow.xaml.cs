using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SnClient.Utils;

namespace SnClient.Pages;

public partial class DebugWindow : ContentPage
{
    public DebugWindow()
    {
        InitializeComponent();
        BindingContext = DebugLogger.Instance; // Bind to the singleton logger
    }
}