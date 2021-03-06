using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using CnSharp.Windows.Updater.Util;
using EnvDTE;
using EnvDTE80;
using Extensibility;
using Microsoft.VisualStudio.CommandBars;
using VSLangProj80;
using Process = System.Diagnostics.Process;

namespace CnSharp.Windows.Updater.SharpPack
{
    /// <summary>用于实现外接程序的对象。</summary>
    /// <seealso class='IDTExtensibility2' />
    public class Connect : IDTExtensibility2, IDTCommandTarget
    {
        #region Constants and Fields

        private const string ProjectNodeName = "Project";
        protected readonly Logger Logger = Common.MyLogger;
        protected CommandBarControl BuildMenu;
        protected DTE2 Dte;
        protected Project Project;
        protected Solution2 Solution;
        protected SolutionBuild2 SolutionBuild;
        protected VSProject2 VsProject;
        private AddIn _addIn;
        private ProductInfo _productToRelease;
        protected CacheHelper<ProjectCache> CacheHelper;
        protected ProjectCache ProjectCache;
        protected ReleaseList ReleaseList;
        protected string BuildDir;
        private int _rootLength;

        #endregion

        #region props

        public string CommandName { get; set; }
        public string MenuText { get; set; }
        public string BuildSuccessText { get; set; }
        public string HotKey { get; set; }
        public int FaceId { get; set; }

        #endregion

        public Connect()
        {
            CommandName = "PackagePublish";
            MenuText = "Package and Publish";
            BuildSuccessText = "Build Successfully.";
            FaceId = 2171;
        }

        #region Public Methods

        #region IDTCommandTarget Members

        /// <summary>实现 IDTCommandTarget 接口的 Exec 方法。此方法在调用该命令时调用。</summary>
        /// <param name="commandName">要执行的命令的名称。</param>
        /// <param name="executeOption">描述该命令应如何运行。</param>
        /// <param name="varIn">从调用方传递到命令处理程序的参数。</param>
        /// <param name="varOut">从命令处理程序传递到调用方的参数。</param>
        /// <param name="handled">通知调用方此命令是否已被处理。</param>
        public void Exec(string commandName, vsCommandExecOption executeOption, ref object varIn, ref object varOut,
                         ref bool handled)
        {
            handled = false;
            if (executeOption == vsCommandExecOption.vsCommandExecOptionDoDefault)
            {
                string shortName = commandName.Substring(_addIn.ProgID.Length + 1);
                if (shortName == CommandName)
                {
                    Build();
                    handled = true;
                }
            }
        }

        /// <summary>实现 IDTCommandTarget 接口的 QueryStatus 方法。此方法在更新该命令的可用性时调用</summary>
        /// <param name="commandName">要确定其状态的命令的名称。</param>
        /// <param name="neededText">该命令所需的文本。</param>
        /// <param name="status">该命令在用户界面中的状态。</param>
        /// <param name="commandText">neededText 参数所要求的文本。</param>
        /// <seealso class='Exec' />
        public void QueryStatus(string commandName, vsCommandStatusTextWanted neededText, ref vsCommandStatus status,
                                ref object commandText)
        {
            if (neededText == vsCommandStatusTextWanted.vsCommandStatusTextWantedNone)
            {
                string shortName = commandName.Substring(_addIn.ProgID.Length + 1);
                //commandName == addInInstance.ProgID + "." + CommandName
                if (CommandName == shortName)
                {
                    status = vsCommandStatus.vsCommandStatusEnabled | vsCommandStatus.vsCommandStatusSupported;
                }
                else
                {
                    status = vsCommandStatus.vsCommandStatusUnsupported;
                }
            }
        }

        #endregion

        #region IDTExtensibility2 Members

        /// <summary>实现 IDTExtensibility2 接口的 OnAddInsUpdate 方法。当外接程序集合已发生更改时接收通知。</summary>
        /// <param name="custom">特定于宿主应用程序的参数数组。</param>
        /// <seealso class='IDTExtensibility2' />
        public void OnAddInsUpdate(ref Array custom)
        {
        }

        /// <summary>实现 IDTExtensibility2 接口的 OnBeginShutdown 方法。接收正在卸载宿主应用程序的通知。</summary>
        /// <param name="custom">特定于宿主应用程序的参数数组。</param>
        /// <seealso class='IDTExtensibility2' />
        public void OnBeginShutdown(ref Array custom)
        {
        }

        /// <summary>实现 IDTExtensibility2 接口的 OnConnection 方法。接收正在加载外接程序的通知。</summary>
        /// <param name="application">宿主应用程序的根对象。</param>
        /// <param name="connectMode">描述外接程序的加载方式。</param>
        /// <param name="addInInst">表示此外接程序的对象。</param>
        /// <param name="custom"></param>
        public void OnConnection(object application, ext_ConnectMode connectMode, object addInInst, ref Array custom)
        {
            Dte = (DTE2)application;
            _addIn = (AddIn)addInInst;
            var commands = (Commands2)Dte.Commands;

            //var rm = new ResourceManager("CnSharp.Windows.Updater.SharpPack.CommandBar",
            //                             Assembly.GetExecutingAssembly());
            //string lang = CultureInfo.CurrentCulture.Name;
            //if (lang == "zh-CN")
            //{
            //    lang = "zh-CHS";
            //}
            //_menuText = rm.GetString(lang + MenuName, CultureInfo.CurrentCulture);
            //_buildSuccessText = rm.GetString(lang + BuildSuccessName, CultureInfo.CurrentCulture);

            var cmdBars = (CommandBars)(Dte.DTE.CommandBars);
            Microsoft.VisualStudio.CommandBars.CommandBar vsBarProject = cmdBars[ProjectNodeName];

            try
            {
                var contextGUID = new object[] { };
                //Add a command to the Commands collection:
                Command command = null;
                try
                {
                    command = commands.Item(_addIn.ProgID + "." + CommandName);
                    if (command != null)
                    {
                        command.Delete();
                    }
                }
                catch
                {
                }
                //if(command == null)
                command = commands.AddNamedCommand2(_addIn, CommandName, MenuText, MenuText, true, FaceId,
                                                    ref contextGUID);
                //Add a control for the command to the tools menu:
                if (command != null)
                {
                    BuildMenu = (CommandBarControl)command.AddControl(vsBarProject, vsBarProject.Controls.Count + 1);
                    if (!string.IsNullOrEmpty(HotKey))
                        command.Bindings = string.Format("Global::{0}", HotKey);
                }
            }
            catch (ArgumentException argEx)
            {
                //System.Diagnostics.Debug.Write("Exception in append menu item:" + argEx.ToString());
                Logger.WriteDebugLog(argEx.Message + argEx.StackTrace);
            }
        }

        /// <summary>实现 IDTExtensibility2 接口的 OnDisconnection 方法。接收正在卸载外接程序的通知。</summary>
        /// <param name="disconnectMode">描述外接程序的卸载方式。</param>
        /// <param name="custom">特定于宿主应用程序的参数数组。</param>
        /// <seealso class='IDTExtensibility2' />
        public void OnDisconnection(ext_DisconnectMode disconnectMode, ref Array custom)
        {
            try
            {
                switch (disconnectMode)
                {
                    case ext_DisconnectMode.ext_dm_HostShutdown:
                    case ext_DisconnectMode.ext_dm_UserClosed:
                        if (BuildMenu != null)
                        {
                            BuildMenu.Delete(true);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteExceptionLog("Disconnection error:" + ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }

        /// <summary>实现 IDTExtensibility2 接口的 OnStartupComplete 方法。接收宿主应用程序已完成加载的通知。</summary>
        /// <param name="custom">特定于宿主应用程序的参数数组。</param>
        /// <seealso class='IDTExtensibility2' />
        public void OnStartupComplete(ref Array custom)
        {
        }

        #endregion

        public void OutputMessage(string outputStr)
        {
            //get output window
            OutputWindow ow = Dte.ToolWindows.OutputWindow;
            //create own pane type
            OutputWindowPane outputPane = null;
            foreach (OutputWindowPane pane in ow.OutputWindowPanes)
            {
                if (pane.Name == CommandName)
                {
                    outputPane = pane;
                    break;
                }
            }
            if (outputPane == null)
                outputPane = ow.OutputWindowPanes.Add(CommandName);
            //output message
            outputPane.OutputString(outputStr);
            outputPane.Activate();
        }

        #endregion

        #region Methods


        protected virtual bool ExecuteBeforeBuild()
        {
            var projects = new List<Project>();
            foreach (Reference3 r in VsProject.References)
            {
                if (r.SourceProject != null)
                    projects.Add(r.SourceProject);
            }
            var fv = new VersionEditForm(Project, projects);
            var ok = (fv.ShowDialog() == DialogResult.OK);
            if (ok)
                _productToRelease = fv.ProductToRelease;
            return ok;
        }

        protected virtual bool IsFileExcluded(string file)
        {
            var ext = Path.GetExtension(file).ToLower();
            var fileName = Path.GetFileName(file).ToLower();
            return (ext == ".pdb" || ext == ".log" || file.Contains(".vshost.") || fileName == "updater.exe" || fileName == "releaselist.xml");
        }

        protected virtual List<FileListItem> GatherFiles(string dir)
        {
            _rootLength = dir.Length;
            return GatherFilesInFolder(dir, true);
        }

        private List<FileListItem> GatherFilesInFolder(string dir, bool isFirst)
        {
            var list = new List<FileListItem>();
            var dirShortName = dir.Substring(_rootLength);
            var folderExcluded = ProjectCache != null && ProjectCache.ExcludeFolders.Contains(dirShortName);
            if (!isFirst)
            {
                var folderItem = new FileListItem { Dir = dir, Selected = !folderExcluded };
                list.Add(folderItem);
            }

            var files = Directory.GetFiles(dir);
            foreach (var file in files)
            {
                if (!IsFileExcluded(file))
                {
                    var shortName = dir.Substring(_rootLength);
                    var selected = !folderExcluded && (ProjectCache == null || !ProjectCache.ExcludeFiles.Contains(shortName));
                    list.Add(new FileListItem
                    {
                        Dir = file,
                        IsFile = true,
                        Selected = selected
                    });
                }
            }

            var folders = Directory.GetDirectories(dir);
            foreach (var folder in folders)
            {
                list.AddRange(GatherFilesInFolder(folder, false).ToArray());
            }
            return list;
        }

        protected virtual bool BuildReleaseList()
        {
            BuildDir = Path.GetDirectoryName(Project.FullName) + "\\bin\\" + SolutionBuild.ActiveConfiguration.Name +
                        "\\";
            var cacheDir = Common.GetHostDir() + "\\_Projects\\" + FileUtil.MD5(Project.FullName) + "\\project.xml";
            CacheHelper = new CacheHelper<ProjectCache>();
            try
            {
                ProjectCache = CacheHelper.Get(cacheDir);
            }
            catch
            {
                ProjectCache = new ProjectCache();
            }
            var files = GatherFiles(BuildDir);
            ////string releaseFileName = Path.GetDirectoryName(prj.FullName) + "\\" + Common.ReleaseConfigFileName;

            var releaseFileName = Path.GetDirectoryName(Project.FullName) + "\\bin\\" + Common.ReleaseConfigFileName;

            if (File.Exists(releaseFileName))
                ReleaseList = FileUtil.ReadReleaseList(releaseFileName);
            if (ReleaseList == null)
            {
                var icons = Directory.GetFiles(BuildDir, "*.ico");
                ReleaseList = new ReleaseList
                {
                    AppName = _productToRelease.Name,
                    Company = _productToRelease.CompanyName,
                    ApplicationStart = Common.GetAssemblyName(Project) + ".exe",
                    ShortcutIcon = icons.Length > 0 ? Path.GetFileName(icons[0]) : ""
                };
            }
            ReleaseList.ReleaseVersion = _productToRelease.Version;
            ReleaseList.MinVersion = _productToRelease.Version;

            var f = new ReleaseForm(BuildDir, files, ReleaseList);

            if (f.ShowDialog() == DialogResult.OK)
            {
                if (ProjectCache == null)
                    ProjectCache = new ProjectCache();
                ProjectCache.ExcludeFiles = f.GetExcludedFiles().ToList();
                ProjectCache.ExcludeFolders = f.GetExcludedFolders().ToList();
                CacheHelper.Save(ProjectCache, cacheDir);
                return true;
            }
            return false;
        }


        protected virtual void ExecuteAfterRelease()
        {
            var di = new DirectoryInfo(BuildDir);
            OutputMessage(string.Format("{0}:{1}", BuildSuccessText, di.Parent.FullName) + Environment.NewLine);
            Process.Start(di.Parent.FullName);
        }


        private void Build()
        {
            try
            {
                //  _applicationObject.ExecuteCommand("Build.BuildSelection", "");
                Project = (Project)((Array)Dte.ActiveSolutionProjects).GetValue(0);
                VsProject = ((VSProject2)(Project.Object));

                var ok = ExecuteBeforeBuild();
                if (!ok)
                    return;

                Solution = (Solution2)Dte.Solution;
                SolutionBuild = (SolutionBuild2)Solution.SolutionBuild;

                //sb2.BuildProject(sb2.ActiveConfiguration.Name, prj.UniqueName, true);
                SolutionBuild.Build(true);
                //_dte.ExecuteCommand("Build.RebuildSolution");

                if (SolutionBuild.LastBuildInfo != 0)
                {
                    return;
                }

                if (!BuildReleaseList())
                    return;

                ExecuteAfterRelease();
            }
            catch (Exception ex)
            {
                OutputMessage(ex.Message);
                Logger.WriteExceptionLog(ex.Message + Environment.NewLine + ex.StackTrace);
                MessageBox.Show(ex.Message + Environment.NewLine + ex.StackTrace, Common.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

    }
}