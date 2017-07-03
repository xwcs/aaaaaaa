using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace app.nuspecutils
{
    class Program
    {
        static void Main(string[] args)
        {
            if(args.Length == 1)
            {
                // doms
                XmlDocument proj = new XmlDocument();
                proj.Load(string.Format("{0}.csproj", args[0]));
                XmlNode projRoot = proj.DocumentElement;

                string nuFile = string.Format("{0}.nuspec", args[0]);

                /*
                 * If there is no *.nuspec do one default
                 * <?xml version="1.0"?>
                    <package>
                      <metadata>
                        <id>xwcs.indesign</id>
                        <version>$version$</version>
                        <title>$title$</title>
                        <authors>...</authors>
                        <!--owners>...</owners>
                        <licenseUrl>...</licenseUrl>
                        <projectUrl>...</projectUrl>
                        <iconUrl>...</iconUrl-->
                        <requireLicenseAcceptance>false</requireLicenseAcceptance>
                        <description></description>
                        <releaseNotes>First release</releaseNotes>
                        <copyright>Copyright 2017</copyright>
                        <tags></tags>
                      </metadata>
                    </package>
                 */
                XmlDocument nuspec = new XmlDocument();
                if (File.Exists(nuFile))
                {
                    nuspec.Load(nuFile);
                }
                else
                {
                    nuspec.LoadXml(string.Format(@"<?xml version=""1.0""?>
                <package>
                  <metadata>
                    <id>{0}</id>
                    <version>$version$</version>
                    <title>$title$</title>
                    <authors>...</authors>
                    <!--owners>...</owners>
                    <licenseUrl>...</licenseUrl>
                    <projectUrl>...</projectUrl>
                    <iconUrl>...</iconUrl-->
                    <requireLicenseAcceptance>false</requireLicenseAcceptance>
                    <description>{0}</description>
                    <releaseNotes>First release</releaseNotes>
                    <copyright>Copyright 2017</copyright>
                    <tags>{0}</tags>
                  </metadata>
                </package>", Path.GetFileName(args[0])));
                }

                XmlNode nuspecRoot = nuspec.DocumentElement;

                string taFile = string.Format("{0}.targets", args[0]);

                XmlDocument targets = new XmlDocument();
                targets.LoadXml(@"<?xml version=""1.0""?><Project xmlns = ""http://schemas.microsoft.com/developer/msbuild/2003""/>");
                XmlNode targetsRoot = targets.DocumentElement;

                // Add the namespace.
                XmlNamespaceManager nsmgr = new XmlNamespaceManager(proj.NameTable);
                nsmgr.AddNamespace("x", projRoot.NamespaceURI);

                // Add the namespace.
                XmlNamespaceManager tansmgr = new XmlNamespaceManager(targets.NameTable);
                tansmgr.AddNamespace("x", targetsRoot.NamespaceURI);

                // new nodes
                XmlElement ndest = nuspec.CreateElement("files");
                XmlElement tdest = targets.CreateElement("ItemGroup", targetsRoot.NamespaceURI);

                //references
                XmlNodeList nodes = projRoot.SelectNodes(@"x:ItemGroup/x:None/x:CopyToOutputDirectory[text()='Always']/../@Include", nsmgr);



                // create content
                foreach (XmlNode n in nodes)
                {
                    //<file src="..." target="Build\..." />
                    XmlElement f = nuspec.CreateElement("file");
                    f.SetAttribute("src", n.Value);
                    f.SetAttribute("target", "Build\\" + n.Value);
                    ndest.AppendChild(f);

                    /*
                     <None Include="$(MSBuildThisFileDirectory)...">
                      <Link>...</Link>
                      <CopyToOutputDirectory>Always</CopyToOutputDirectory>
                    </None>
                     */
                    f = targets.CreateElement("None", targetsRoot.NamespaceURI);
                    f.SetAttribute("Include", "$(MSBuildThisFileDirectory)" + n.Value);
                    XmlElement ln = targets.CreateElement("Link", targetsRoot.NamespaceURI);
                    ln.InnerText = n.Value;
                    XmlElement ctn = targets.CreateElement("CopyToOutputDirectory", targetsRoot.NamespaceURI);
                    ctn.InnerText = "Always";
                    f.AppendChild(ln);
                    f.AppendChild(ctn);
                    tdest.AppendChild(f);
                }

                // targets
                string ff = Path.GetFileName(args[0]) + ".targets";
                XmlElement fn = nuspec.CreateElement("file");
                fn.SetAttribute("src", ff);
                fn.SetAttribute("target", "Build\\" + ff);
                ndest.AppendChild(fn);

                //destination 1
                XmlNode dest = nuspecRoot.SelectSingleNode(@"//files");
                if (!ReferenceEquals(null, dest))
                {
                    dest.ParentNode.RemoveChild(dest);
                }

                // add new nodes
                nuspecRoot.AppendChild(ndest);

                // backup
                try
                {
                    File.Copy(nuFile, nuFile + ".bak", true);
                }
                catch (Exception) { }


                nuspec.Save(nuFile);

                //destination 2
                XmlNode ttdest = targetsRoot.SelectSingleNode(@"//x:ItemGroup", tansmgr);
                if (!ReferenceEquals(null, ttdest))
                {
                    ttdest.ParentNode.RemoveChild(ttdest);
                }

                // add new nodes
                targetsRoot.AppendChild(tdest);

                // backup
                try
                {
                    File.Copy(taFile, taFile + ".bak", true);
                }
                catch (Exception) { }


                targets.Save(taFile);
            }

            else if (args.Length == 3 && args[1] == "-V")
            {
                string nuFile = string.Format("{0}.nuspec", args[0]);
                string dllFile = string.Format("{0}\\{1}", Path.GetDirectoryName(args[0]), args[2]);
                XmlDocument nuspec = new XmlDocument();
                nuspec.Load(nuFile);
                
                XmlNode nuspecRoot = nuspec.DocumentElement;
                // set version

                try
                {
                    string path = Path.GetFullPath(dllFile);
                    var assembly = Assembly.LoadFile(path);
                    Console.Out.WriteLine(assembly.GetName().FullName);
                    string vv = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
                    if(vv == null)
                    {
                        string[] parts = assembly.GetName().FullName.Split(',');
                        vv = parts[1].Split('=')[1];
                    }

                    XmlNode vn = nuspecRoot.SelectSingleNode("//version");
                    if (!ReferenceEquals(null, vn))
                    {
                        vn.InnerText = vv;
                    }

                    try
                    {
                        File.Copy(nuFile, nuFile + ".bak", true);
                    }
                    catch (Exception) { }


                    nuspec.Save(nuFile);
                }
                catch (Exception exception)
                {
                    Console.Out.WriteLine(string.Format("{0}: {1}", dllFile, exception.Message));
                }

                

                return;
            }else
            {
                Console.WriteLine("Usage : app.nuspecutils <projectname>");
                Console.WriteLine("        app.nuspecutils <projectname> -V <dll path>");
            }
        }
    }
}
