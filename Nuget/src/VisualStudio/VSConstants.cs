namespace NuGet.VisualStudio
{
    internal static class VsConstants
    {
        // Project type guids
        internal const string WebApplicationProjectTypeGuid = "{349C5851-65DF-11DA-9384-00065B846F21}";
        internal const string WebSiteProjectTypeGuid = "{E24C65DC-7377-472B-9ABA-BC803B73C61A}";
        internal const string CsharpProjectTypeGuid = "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}";
        internal const string VbProjectTypeGuid = "{F184B08F-C81C-45F6-A57F-5ABD9991F28F}";
        internal const string FsharpProjectTypeGuid = "{F2A71F9B-5D33-465A-A702-920D77279786}";
        internal const string JsProjectTypeGuid = "{262852C6-CD72-467D-83FE-5EEB1973A190}";
        internal const string WixProjectTypeGuid = "{930C7802-8A8C-48F9-8165-68863BCCD9DD}";
        internal const string LightSwitchProjectTypeGuid = "{ECD6D718-D1CF-4119-97F3-97C25A0DFBF9}";

        // Copied from EnvDTE.Constants since that type can't be embedded
        internal const string VsProjectItemKindPhysicalFile = "{6BB5F8EE-4483-11D3-8BCF-00C04F8EC28C}";
        internal const string VsProjectItemKindPhysicalFolder = "{6BB5F8EF-4483-11D3-8BCF-00C04F8EC28C}";
        internal const string VsProjectItemKindSolutionFolder = "{66A26720-8FB5-11D2-AA7E-00C04F688DDE}";
        internal const string VsWindowKindSolutionExplorer = "{3AE79031-E1BC-11D0-8F78-00A0C9110057}";

        // All unloaded projects have this Kind value
        internal const string UnloadedProjectTypeGuid = "{67294A52-A4F0-11D2-AA88-00C04F688DDE}";

        internal const string NuGetSolutionSettingsFolder = ".nuget";

        // HResults
        internal const int S_OK = 0;
    }
}
