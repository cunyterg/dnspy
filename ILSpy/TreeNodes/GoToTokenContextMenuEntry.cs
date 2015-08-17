﻿/*
    Copyright (C) 2014-2015 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System.ComponentModel.Composition;
using System.Windows.Input;
using dnlib.DotNet;
using dnlib.IO;
using dnSpy.AsmEditor;
using dnSpy.Tabs;
using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.TreeNodes;

namespace dnSpy.TreeNodes {
	[Export(typeof(IPlugin))]
	sealed class GoToTokenPlugin : IPlugin {
		public void OnLoaded() {
			MainWindow.Instance.CodeBindings.Add(new RoutedCommand("GoToToken", typeof(GoToTokenPlugin)),
				(s, e) => GoToTokenContextMenuEntry.Execute(),
				(s, e) => e.CanExecute = GoToTokenContextMenuEntry.CanExecute(),
				ModifierKeys.Control, Key.D);
            MainWindow.Instance.CodeBindings.Add(new RoutedCommand("GoToFileOffset", typeof(GoToTokenPlugin)),
                (s, e) => GoToOffsetContextMenuEntry.Execute(),
                (s, e) => e.CanExecute = GoToOffsetContextMenuEntry.CanExecute(),
                ModifierKeys.Alt, Key.G);
		}
	}

	[ExportContextMenuEntryAttribute(Header = "Go to M_D Token…", Order = 400, Category = "Tokens", InputGestureText = "Ctrl+D")]
	sealed class GoToTokenContextMenuEntry : IContextMenuEntry {
		public bool IsVisible(TextViewContext context) {
			return CanExecute() &&
				(context.SelectedTreeNodes != null || context.TextView != null);
		}

		public bool IsEnabled(TextViewContext context) {
			return true;
		}

		public void Execute(TextViewContext context) {
			Execute();
		}

		static ModuleDefMD GetModule(out DecompileTabState tabState) {
			tabState = MainWindow.Instance.GetActiveDecompileTabState();
			if (tabState == null)
				return null;
			return ILSpyTreeNode.GetModule(tabState.DecompiledNodes) as ModuleDefMD;
		}

		internal static bool CanExecute() {
			DecompileTabState tabState;
			return GetModule(out tabState) != null;
		}

		internal static void Execute() {
			DecompileTabState tabState;
			var module = GetModule(out tabState);
			if (module == null)
				return;

			var ask = new AskForInput();
			ask.Owner = MainWindow.Instance;
			ask.Title = "Go to MD Token";
			ask.label.Content = "_Metadata token";
			ask.textBox.Text = "";
			ask.textBox.ToolTip = "Enter an MD token: 0x06001234 or 0x0200ABCD";
			ask.ShowDialog();
			if (ask.DialogResult != true)
				return;
			string tokenText = ask.textBox.Text;
			tokenText = tokenText.Trim();
			if (string.IsNullOrEmpty(tokenText))
				return;

			string error;
			uint token = NumberVMUtils.ParseUInt32(tokenText, uint.MinValue, uint.MaxValue, out error);
			if (!string.IsNullOrEmpty(error)) {
				MainWindow.Instance.ShowMessageBox(error);
				return;
			}

			var memberRef = module.ResolveToken(token) as IMemberRef;
			var member = MainWindow.ResolveReference(memberRef);
			if (member == null) {
				if (memberRef == null)
					MainWindow.Instance.ShowMessageBox(string.Format("Invalid metadata token: 0x{0:X8}", token));
				else
					MainWindow.Instance.ShowMessageBox(string.Format("Could not resolve member reference token: 0x{0:X8}", token));
				return;
			}

			MainWindow.Instance.JumpToReference(tabState.TextView, member);
		}
	}

    [ExportContextMenuEntryAttribute(Header = "Go to File(RVA) O_ffset", Order = 401, Category = "Offset", InputGestureText = "Alt+G")]
    sealed class GoToOffsetContextMenuEntry : IContextMenuEntry
    {
        public bool IsVisible(TextViewContext context)
        {
            return CanExecute() &&
                (context.SelectedTreeNodes != null || context.TextView != null);
        }

        public bool IsEnabled(TextViewContext context)
        {
            return true;
        }

        public void Execute(TextViewContext context)
        {
            Execute();
        }

        static ModuleDefMD GetModule(out DecompileTabState tabState)
        {
            tabState = MainWindow.Instance.GetActiveDecompileTabState();
            if (tabState == null)
                return null;
            return ILSpyTreeNode.GetModule(tabState.DecompiledNodes) as ModuleDefMD;
        }

        internal static bool CanExecute()
        {
            DecompileTabState tabState;
            return GetModule(out tabState) != null;
        }

        internal static void Execute()
        {
            DecompileTabState tabState;
            var module = GetModule(out tabState);
            if (module == null)
                return;

            var ask = new AskForInput();
            ask.Owner = MainWindow.Instance;
            ask.Title = "Go to File(RVA) Offset";
            ask.label.Content = "_File(RVA) offset";
            ask.textBox.Text = "";
            ask.textBox.ToolTip = "Enter an File(RVA) offset: 0x1234(0x1234*) or 0x0ABCD(0x0ABCD*)";
            ask.ShowDialog();
            if (ask.DialogResult != true)
                return;
            string fileOffset = ask.textBox.Text;
            fileOffset = fileOffset.Trim();
            if (string.IsNullOrEmpty(fileOffset))
                return;

            bool isRVA = false;
            if (fileOffset.EndsWith("*"))
            {
                fileOffset = fileOffset.Replace("*","");
                isRVA = true;
            }
            string error;
            ulong offset = NumberVMUtils.ParseUInt64(fileOffset, ulong.MinValue, ulong.MaxValue, out error);
            if (!string.IsNullOrEmpty(error))
            {
                MainWindow.Instance.ShowMessageBox(error);
                return;
            }

            IMemberDef memberRef = null;
                foreach (var types in module.GetTypes())
                {
                    foreach (var md in types.Methods)
                    {
                        if (GetMemberRef(md, isRVA, module, offset, ref memberRef)) goto br;
                    }
                    foreach (var nestedType in types.GetNestedTypes(true))
                    {
                        foreach (var md in nestedType.GetMethods(true))
                        {
                            if (GetMemberRef(md, isRVA, module, offset, ref memberRef)) goto br;
                        }
                    }
                    foreach (var propertyDef in types.Properties)
                    {
                        foreach (var md in propertyDef.GetMethods)
                        {
                            if (GetMemberRef(md, isRVA, module, offset, ref memberRef)) goto br;
                        }
                    }
                    foreach (var eventDef in types.Events)
                    {
                        foreach (var md in eventDef.OtherMethods)
                        {
                            if (GetMemberRef(md, isRVA, module, offset, ref memberRef)) goto br;
                        }
                    }
                }
            br:
            var member = MainWindow.ResolveReference(memberRef);
            if (member == null)
            {
                if (memberRef == null)
                    MainWindow.Instance.ShowMessageBox(string.Format("Offset: 0x{0:X8} isn't in methods" , offset));
                else
                    MainWindow.Instance.ShowMessageBox(string.Format("Could not resolve member reference offset: 0x{0:X8}", offset));
                return;
            }

            MainWindow.Instance.JumpToReference(tabState.TextView, member);
        }

        private static bool GetMemberRef(MethodDef md, bool isRVA, ModuleDefMD module, ulong offset,
            ref IMemberDef memberRef)
        {
            if (md == null)
                return true;
            var body = md.Body;
            if (body == null)
                return true;

            var len = AsmEditor.Hex.InstructionUtils.GetTotalMethodBodyLength(md);
            offset = isRVA ? offset : (ulong) module.MetaData.PEImage.ToRVA((FileOffset) offset);
            var add = (ulong)md.RVA;
            if (offset >= add && offset < add + len)
            {
                memberRef = md;
                return true;
            }
            return false;
        }
    }
}
