﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.239
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace SpellingFixerEC.Properties {
	using System;


	/// <summary>
	///   A strongly-typed resource class, for looking up localized strings, etc.
	/// </summary>
	// This class was auto-generated by the StronglyTypedResourceBuilder
	// class via a tool like ResGen or Visual Studio.
	// To add or remove a member, edit your .ResX file then rerun ResGen
	// with the /str option, or rebuild your VS project.
	[global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
	[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
	[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
	internal class Resources {

		private static global::System.Resources.ResourceManager resourceMan;

		private static global::System.Globalization.CultureInfo resourceCulture;

		[global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
		internal Resources() {
		}

		/// <summary>
		///   Returns the cached ResourceManager instance used by this class.
		/// </summary>
		[global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
		internal static global::System.Resources.ResourceManager ResourceManager {
			get {
				if (object.ReferenceEquals(resourceMan, null)) {
					global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("SpellingFixerEC.Properties.Resources", typeof(Resources).Assembly);
					resourceMan = temp;
				}
				return resourceMan;
			}
		}

		/// <summary>
		///   Overrides the current thread's CurrentUICulture property for all
		///   resource lookups using this strongly typed resource class.
		/// </summary>
		[global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
		internal static global::System.Globalization.CultureInfo Culture {
			get {
				return resourceCulture;
			}
			set {
				resourceCulture = value;
			}
		}

		/// <summary>
		///   Looks up a localized string similar to
		///The &quot;Bad Spelling&quot; box shows a misspelled word in one of your documents. By creating a
		///&quot;bad to good&quot; replacment rule, you are saying, in effect, &quot;Every time this Bad Spelling
		///form occurs, it should be changed to the following Replacement form.&quot;
		///
		///This box supports &quot;Alt+X&quot; behavior: If you enter the 4 hex digits of a Unicode code point,
		///and type Alt+X, it will convert those four digits into the corresponding Unicode character
		///(e.g. &quot;0061&quot; followed by Alt+X will result in the latin letter a).
		///    .
		/// </summary>
		internal static string textBoxBadWordHelp {
			get {
				return ResourceManager.GetString("textBoxBadWordHelp", resourceCulture);
			}
		}

		/// <summary>
		///   Looks up a localized string similar to
		///The &quot;Replacement&quot; box shows the correct spelling for the misspelled word in the &quot;Bad Spelling&quot;
		///box. By creating a &quot;bad to good&quot; replacment rule, you are saying, in effect, &quot;Every time the
		///Bad Spelling form occurs, it should be changed to this Replacement form.&quot;
		///
		///This box supports &quot;Alt+X&quot; behavior: If you enter the 4 hex digits of a Unicode code point,
		///and type Alt+X, it will convert those four digits into the corresponding Unicode character
		///(e.g. &quot;0061&quot; followed by Alt+X will result in the latin [rest of string was truncated]&quot;;.
		/// </summary>
		internal static string textBoxReplacementHelp {
			get {
				return ResourceManager.GetString("textBoxReplacementHelp", resourceCulture);
			}
		}

		/// <summary>
		///   Looks up a localized string similar to
		///Select the word you want to change and press the F2 key to edit.
		///When you are finished, press the Enter key to save the value.
		///
		///You can also right click on a row to edit the words with the
		///&apos;Fix Spelling&apos; window.
		///
		///To delete a row, select it and press the Delete key.
		///
		///To add a row, click the &apos;Add Correction&apos; button at the bottom of the window.
		///    .
		/// </summary>
		internal static string ViewBadGoodPairsBad2GoodHelp {
			get {
				return ResourceManager.GetString("ViewBadGoodPairsBad2GoodHelp", resourceCulture);
			}
		}
	}
}
