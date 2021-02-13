using System.Windows;
using System.Reflection;
using System.Runtime.CompilerServices;
using VMS.TPS.Common.Model.API;
using SimpleGui;
using EsapiEssentials.Plugin;
using SimpleGui.Views;
using SimpleGui.ViewModels;

[assembly: ESAPIScript(IsWriteable = true)]
namespace VMS.TPS
{
    public class Script : ScriptBase
    {
        public Script()
        {
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public override void Run(PluginScriptContext context)
        {
            if (context.Patient == null || context.ExternalPlanSetup == null || context.StructureSet == null) { MessageBox.Show("Patient, ExternalBeamPlan or StructureSet is missing"); return; }

            // TODO : Add here the code that is called when the script is launched from Eclipse.
            var mainViewModel = new MainViewModel(context.Patient, context.ExternalPlanSetup, context.StructureSet);
            var window = new MainView(mainViewModel);
            window.DataContext = mainViewModel;

            window.ShowDialog();
        }
    }
}