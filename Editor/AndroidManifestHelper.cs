using UnityEditor;
using UnityEngine;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using System.Xml;
using System.IO;
using System.Linq;
#if UNITY_ANDROID
using UnityEditor.Android;
#endif


[InitializeOnLoad]
public class AndroidManifestHelper : IPreprocessBuildWithReport, IPostprocessBuildWithReport
#if UNITY_ANDROID
	, IPostGenerateGradleAndroidProject
#endif
{
    public int callbackOrder => 99999;

#if UNITY_ANDROID
    public void OnPostGenerateGradleAndroidProject(string path)
	{
        string manifestFolder = Path.Combine(path, "src/main");
        string sourceFile = manifestFolder + "/AndroidManifest.xml";
        var doc = new XmlDocument();
        doc.Load(sourceFile);
        var element = (XmlElement)doc.SelectSingleNode("/manifest");
        if (element == null)
        {
            Debug.LogError("Could not find manifest tag in android manifest.");
            return;
        }
        
        var androidNamespaceUri = element.GetAttribute("xmlns:android");
        if (string.IsNullOrEmpty(androidNamespaceUri))
        {
            Debug.LogError("Could not find Android Namespace in manifest.");
            return;
        }
        AddOrRemoveTag(doc, androidNamespaceUri, "/manifest", "uses-permission", "android.permission.CHANGE_WIFI_MULTICAST_STATE", true, false);
        AddOrRemoveTag(doc, androidNamespaceUri, "/manifest", "uses-permission", "android.permission.INTERNET", true, false);
        doc.Save(sourceFile);
    }
#endif

    private static void AddOrRemoveTag(XmlDocument doc, string uri, string path, string elementName, string name, bool required, bool modifyIfFound, params string[] attrs)
    {
        var nodeList = doc.SelectNodes(path + "/" + elementName);
        var element = nodeList?.Cast<XmlElement>().FirstOrDefault(xml => name == null || name == xml.GetAttribute("name", uri));

        if (required)
        {
            if (element == null)
            {
                var parent = doc.SelectSingleNode(path);
                element = doc.CreateElement(elementName);
                element.SetAttribute("name", uri, name);
                parent?.AppendChild(element);
            }

            for (int i = 0; i < attrs.Length; i += 2)
            {
                if (modifyIfFound || string.IsNullOrEmpty(element.GetAttribute(attrs[i], uri)))
                {
                    if (attrs[i + 1] != null)
                    {
                        element.SetAttribute(attrs[i], uri, attrs[i + 1]);
                    }
                    else
                    {
                        element.RemoveAttribute(attrs[i], uri);
                    }
                }
            }
        }
        else
        {
            if (element != null && modifyIfFound)
            {
                element.ParentNode?.RemoveChild(element);
            }
        }
    }

    public void OnPostprocessBuild(BuildReport report) {}
	public void OnPreprocessBuild(BuildReport report) {}
}
