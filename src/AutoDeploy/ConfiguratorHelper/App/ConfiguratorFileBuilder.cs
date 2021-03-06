﻿using Microsoft.Web.Administration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConfiguratorHelper.App
{
    public class ConfiguratorFileBuilder
    {
        public static List<string> CreateConfiguratorFile_Classic(Options options)
        {
            string createClassic = "\"ConfiguratorAutomator.exe\"" + " http://"
                + options.hostURL + ":10000/create_website.asp "
                + options.domain + " " + options.username + " " + options.password + " "
                + "\"selectWeb=1&theWebSite=" + options.classicWebsiteName
                + "&virtual_name=" + options.classicVirtualName
                + "&CBCBversion=1.0.1&thePoolmode=create&thePool="
                + options.applicationPool + "&newAppPoolName=" + AppPoolHelper.GetNewApplicationPoolName(options.applicationPool) + "&CBauth=cb_only&IISannon=on&usernamedefault=on&connectCreate=connect&DBserver="
                + options.dbserver + "&DBport="
                + options.dbPort + "&SAuser=" + options.dbsauser + "&SApass=" + options.dbsapassword + "&DBnameS=" + options.dbname
                + "&DBnameC=&username=" + options.dbusername + "&password=" + options.dbuserpassword + "&adminusername=&adminpassword=&cbversion_modopt="
                + options.cbVersion + "&DBsessions_modopt=true&dbversion_modopt=" + options.dbVersion + "&EnableCaseMap_modopt=on&EnableDocMapper_modopt=on&EnableOCRProcessing_modopt=on&StaticCache_modopt=on"
                + "&Advanced+Search=on&Advanced+Search_mcbv=2.0.0&Advanced_Fulltext_modopt=on&Advanced_Fulltext_Tran_modopt=on&AFTversion_modopt=2.0.12&Analytics+Module=on&Analytics+Module_mcbv=1.0.1"
                + "&Analyticsversion_modopt=1.0.1&Analytics_Module_modopt=true&Content+Compare=on&Content+Compare_mcbv=1.0.1&TextCompare_Module_modopt=false&Court+Module=on"
                + "&Court+Module_mcbv=1.0.1&Courtversion_modopt=1.0.6&court_enable_modopt=true&DocMapper=on&DocMapper_mcbv=1.0.0&DMClientVersion_modopt=5.2.0.0&Master+Dups=on&Master+Dups_mcbv=1.0.1"
                + "&MasterDups_Module_modopt=true&Production+Module=on&Production+Module_mcbv=3.0.0&EnableProduction3_modopt=true&Production3version_modopt=2.0.0&Review+Workflow=on&Review+Workflow_mcbv=1.0.1"
                + "&BatchAssignments_Module_modopt=true&Ringtail+Data+Exchange=on&Ringtail+Data+Exchange_mcbv=1.0.1&RDX_Console_modopt=on&Riv+Server=on&Riv+Server_mcbv=3.0.1"
                + "&EnableRedaction_modopt=true&EnableRivInstall_modopt=on&RivClientVersion_modopt=3980&RIVversion_modopt=3.0.10&Zip+And+Download=on&Zip+And+Download_mcbv=1.0.0"
                + "&ZipAndDownload_modopt=true&ZipAndDownloadVersion_modopt=1.0&theFunction=Edit&vdtype=&admin_action=1&agent=false&rpf=&docMapper=";

            string editClassic = "\"ConfiguratorAutomator.exe\"" + " http://"
               + options.hostURL + ":10000/create_website.asp "
               + options.domain + " " + options.username + " " + options.password + " "
               + "\"selectWeb=1&theWebSite=" + options.classicWebsiteName
               + "&virtual_name=" + options.classicVirtualName
               + "&CBCBversion=1.0.1&thePoolmode=Exists&thePool="
               + options.applicationPool + "&newAppPoolName=&CBauth=cb_only&IISannon=on&usernamedefault=on&connectCreate=connect&DBserver="
               + options.dbserver + "&DBport="
               + options.dbPort + "&SAuser=" + options.dbsauser + "&SApass=" + options.dbsapassword + "&DBnameS=" + options.dbname
               + "&DBnameC=&username=" + options.dbusername + "&password=" + options.dbuserpassword + "&adminusername=&adminpassword=&cbversion_modopt="
               + options.cbVersion + "&DBsessions_modopt=true&dbversion_modopt=" + options.dbVersion + "&EnableCaseMap_modopt=on&EnableDocMapper_modopt=on&EnableOCRProcessing_modopt=on&StaticCache_modopt=on"
               + "&Advanced+Search=on&Advanced+Search_mcbv=2.0.0&Advanced_Fulltext_modopt=on&Advanced_Fulltext_Tran_modopt=on&AFTversion_modopt=2.0.12&Analytics+Module=on&Analytics+Module_mcbv=1.0.1"
               + "&Analyticsversion_modopt=1.0.1&Analytics_Module_modopt=true&Content+Compare=on&Content+Compare_mcbv=1.0.1&TextCompare_Module_modopt=false&Court+Module=on"
               + "&Court+Module_mcbv=1.0.1&Courtversion_modopt=1.0.6&court_enable_modopt=true&DocMapper=on&DocMapper_mcbv=1.0.0&DMClientVersion_modopt=5.2.0.0&Master+Dups=on&Master+Dups_mcbv=1.0.1"
               + "&MasterDups_Module_modopt=true&Production+Module=on&Production+Module_mcbv=3.0.0&EnableProduction3_modopt=true&Production3version_modopt=2.0.0&Review+Workflow=on&Review+Workflow_mcbv=1.0.1"
               + "&BatchAssignments_Module_modopt=true&Ringtail+Data+Exchange=on&Ringtail+Data+Exchange_mcbv=1.0.1&RDX_Console_modopt=on&Riv+Server=on&Riv+Server_mcbv=3.0.1"
               + "&EnableRedaction_modopt=true&EnableRivInstall_modopt=on&RivClientVersion_modopt=3980&RIVversion_modopt=3.0.10&Zip+And+Download=on&Zip+And+Download_mcbv=1.0.0"
               + "&ZipAndDownload_modopt=true&ZipAndDownloadVersion_modopt=1.0&theFunction=Edit&vdtype=&admin_action=1&agent=false&rpf=&docMapper=";


            var list = new List<string>();
            list.Add(createClassic);
            list.Add(editClassic);
            return list;

        }

        public static List<string> CreateConfiguratorFile_Agent(Options options)
        {
            var list = new List<string>();

            // CREATE IT.
            string agent = "\"ConfiguratorAutomator.exe\"" + " http://"
                + options.hostURL + ":10000/create_website.asp "
                + options.domain + " " + options.username + " " + options.password + " "
                + "\"selectWeb=1&theWebSite=" + options.classicWebsiteName + "&virtual_name=" + options.agentVirtualName + "&AGtype=Primary&CBCBversion=1.0.1&thePoolmode=create&thePool="
                + options.agentApplicationPool + "&newAppPoolName=" + AppPoolHelper.GetNewApplicationPoolName(options.agentApplicationPool) + "&IUSR_username=IUSR&IUSR_password=&IISbasic=on&theFunction=Edit&vdtype=&admin_action=1&agent=true&rpf=&docMapper=";

            list.Add(agent);

            // EDIT IT.
            agent = "\"ConfiguratorAutomator.exe\"" + " http://"
            + options.hostURL + ":10000/create_website.asp "
            + options.domain + " " + options.username + " " + options.password + " "
            + "\"selectWeb=1&virtual_name=" + options.agentVirtualName + "&AGtype=Primary&CBCBversion=1.0.1&thePoolmode=Exists&thePool=" + options.agentApplicationPool +
            "&newAppPoolName=&IUSR_username=&IUSR_password=&IISbasic=on&theFunction=Edit&vdtype=&admin_action=1&agent=true&rpf=&docMapper=";
            list.Add(agent);

            return list;
        }
    }
}
