﻿/*
Copyright 2011 Google Inc

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Google.Apis.Discovery.v1.Data;
using UpdateWikiLists.Util;

namespace UpdateWikiLists.Wiki
{
    /// <summary>
    /// Updater for the APILibraries wiki page.
    /// </summary>
    public static class APILibraries
    {
        private const string StartTag = "<wiki:comment>BEGIN_GENERATED</wiki:comment>";
        private const string EndTag = "<wiki:comment>END_GENERATED</wiki:comment>";

        /// <summary>
        /// Generates the API listing
        /// </summary>
        public static string GenerateList()
        {
            var tmpl = new Template();
            tmpl.Add("");
            tmpl.Add("----");
            tmpl.Add("");
            tmpl.Add("== {TITLE} ==");
            tmpl.Add("{DESCRIPTION}.");
            tmpl.Add("{CONTENT}");
            tmpl.Add("");
            tmpl.Add("=== Samples ===");
            tmpl.Add("{SAMPLES}");

            var apiGroups = from a in Discovery.ListApis()
                            group a by a.Name
                            into grp select new { Info = grp.First(), APIs = grp };
            var apis = apiGroups.Select(qry => tmpl.ToString(new Entries
                                                           {
                                                               { "TITLE", qry.Info.Title }, 
                                                               { "DESCRIPTION", qry.Info.Description },
                                                               { "CONTENT", GenerateApiVersions(qry.APIs) },
                                                               { "SAMPLES", GenerateSamples(qry.Info.Name) }
                                                           }));
            return apis.Aggregate(TextUtils.JoinLines);
        }

        /// <summary>
        /// Modifies the existing APILibraries wiki file by replacing the "generated" section.
        /// </summary>
        public static void InsertIntoFile(string file)
        {
            TextUtils.InsertIntoFile(file, StartTag, EndTag, GenerateList());
        }

        private static string GenerateApiVersions(IEnumerable<DirectoryList.ItemsData> apis)
        {
            return apis.Select(GenerateApi).Aggregate((a,b) => a + Environment.NewLine + Environment.NewLine + b);
        }

        private static string GenerateApi(DirectoryList.ItemsData api)
        {
            var tmpl = new Template();
            tmpl.Add("|| *{VERSION}* | [{URL_BINARY} {FILE_BINARY}]  ([{URL_SOURCE} Source]) | [{URL_XML} XmlDoc] ||");
            tmpl.Add("|| [{URL_DOCU} Documentation] | " +
                     "[https://code.google.com/apis/explorer/#_s={NAME}&_v={VERSION} APIs Explorer] ||");
            string formalName = api.Name.ToUpperFirstChar();
            string formalVersion = api.Version.ToUpperFirstChar();
            string binFileName = string.Format("Google.Apis.{0}.{1}.dll", formalName, formalVersion);

            // Fill in the data.
            const string RELEASE_DIR = "http://contrib.google-api-dotnet-client.googlecode.com/hg/current";
            const string BINARY_PATH = RELEASE_DIR + "/Generated/Bin/{0}Service/{1}";
            var data = new Entries();
            data.Add("URL_DOCU", api.DocumentationLink);
            data.Add("URL_BINARY", BINARY_PATH, formalName, binFileName);
            data.Add("URL_XML", BINARY_PATH, formalName, Path.ChangeExtension(binFileName, ".xml"));
            data.Add("FILE_BINARY", binFileName);
            data.Add("URL_SOURCE", RELEASE_DIR + "/Generated/Source/{0}Service.cs", formalName);
            data.Add("NAME", api.Name);
            data.Add("VERSION", api.Version);
            return tmpl.ToString(data);
        }

        private static string GenerateSamples(string apiName)
        {
            string path = Program.Samples.WorkingDirectory;
            apiName = apiName.ToUpperFirstChar();

            // List all samples in the sample directory matching the specified api name.
            var samples = Directory.GetDirectories(path, apiName + ".*", SearchOption.TopDirectoryOnly);
            if (samples.Count() == 0)
            {
                return "  * _(There are no samples for this service yet.)_";
            }

            const string format =
                "  * [http://code.google.com/p/google-api-dotnet-client/source/browse?repo=samples#hg%2F{0} {0}]";
            return samples.Select(Path.GetFileName)
                    .Select(dir => string.Format(format, dir))
                    .Aggregate(TextUtils.JoinLines);
        }
    }
}