﻿// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Linq;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.TreeNodes;
using ICSharpCode.TreeView;
using Microsoft.Win32;
using dnlib.DotNet;

namespace TestPlugin
{
	[ExportContextMenuEntryAttribute(Header = "_Save Assembly")]
	public class SaveAssembly : IContextMenuEntry
	{
		public bool IsVisible(ContextMenuEntryContext context)
		{
			return context.SelectedTreeNodes != null && context.SelectedTreeNodes.All(n => n is AssemblyTreeNode);
		}
		
		public bool IsEnabled(ContextMenuEntryContext context)
		{
			return context.SelectedTreeNodes != null && context.SelectedTreeNodes.Length == 1;
		}
		
		public void Execute(ContextMenuEntryContext context)
		{
			if (context.SelectedTreeNodes == null)
				return;
			AssemblyTreeNode node = (AssemblyTreeNode)context.SelectedTreeNodes[0];
			var mod = node.LoadedAssembly.ModuleDefinition;
			if (mod != null) {
				SaveFileDialog dlg = new SaveFileDialog();
				dlg.FileName = node.LoadedAssembly.FileName;
				dlg.Filter = "Assembly|*.dll;*.exe";
				if (dlg.ShowDialog(MainWindow.Instance) == true) {
					mod.Write(dlg.FileName);
				}
			}
		}
	}
}
