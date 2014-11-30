﻿/*
    This file is part of HomeGenie Project source code.

    HomeGenie is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    HomeGenie is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with HomeGenie.  If not, see <http://www.gnu.org/licenses/>.  
*/

/*
 *     Author: Generoso Martello <gene@homegenie.it>
 *     Project Homepage: http://homegenie.it
 */

using HomeGenie.Automation;
using MIG;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using HomeGenie.Service;
using System.IO;
using System.Net;
using System.Xml.Serialization;
using Newtonsoft.Json;
using HomeGenie.Service.Constants;
using HomeGenie.Automation.Scheduler;
using Microsoft.Scripting.Hosting;
using Microsoft.Scripting;

namespace HomeGenie.Service.Handlers
{

    public class Automation
    {
        private HomeGenieService homegenie;
        public Automation(HomeGenieService hg)
        {
            homegenie = hg;
        }

        public void ProcessRequest(MIGClientRequest request, MIGInterfaceCommand migCommand)
        {
            string streamcontent = "";
            ProgramBlock currentProgram;
            ProgramBlock newProgram;
            //
            homegenie.ExecuteAutomationRequest(migCommand);
            if (migCommand.Command.StartsWith("Macro."))
            {
                switch (migCommand.Command)
                {
                    case "Macro.Record":
                        homegenie.ProgramEngine.MacroRecorder.RecordingEnable();
                        break;
                    case "Macro.Save":
                        newProgram = homegenie.ProgramEngine.MacroRecorder.SaveMacro(migCommand.GetOption(1));
                        migCommand.Response = newProgram.Address.ToString();
                        break;
                    case "Macro.Discard":
                        homegenie.ProgramEngine.MacroRecorder.RecordingDisable();
                        break;
                    case "Macro.SetDelay":
                        switch (migCommand.GetOption(0).ToLower())
                        {
                            case "none":
                                homegenie.ProgramEngine.MacroRecorder.DelayType = MacroDelayType.None;
                                break;

                            case "mimic":
                                homegenie.ProgramEngine.MacroRecorder.DelayType = MacroDelayType.Mimic;
                                break;

                            case "fixed":
                                double secs = double.Parse(migCommand.GetOption(1), System.Globalization.CultureInfo.InvariantCulture);
                                homegenie.ProgramEngine.MacroRecorder.DelayType = MacroDelayType.Fixed;
                                homegenie.ProgramEngine.MacroRecorder.DelaySeconds = secs;
                                break;
                        }
                        break;
                    case "Macro.GetDelay":
                        migCommand.Response = "[{ DelayType : '" + homegenie.ProgramEngine.MacroRecorder.DelayType + "', DelayOptions : '" + homegenie.ProgramEngine.MacroRecorder.DelaySeconds + "' }]";
                        break;
                }
            }
            else if (migCommand.Command.StartsWith("Scheduling."))
            {
                switch (migCommand.Command)
                {
                    case "Scheduling.Add":
                    case "Scheduling.Update":
                        var item = homegenie.ProgramEngine.SchedulerService.AddOrUpdate(migCommand.GetOption(0), migCommand.GetOption(1).Replace("|", "/"));
                        item.ProgramId = migCommand.GetOption(2);
                        homegenie.UpdateSchedulerDatabase();
                        break;
                    case "Scheduling.Delete":
                        homegenie.ProgramEngine.SchedulerService.Remove(migCommand.GetOption(0));
                        homegenie.UpdateSchedulerDatabase();
                        break;
                    case "Scheduling.Enable":
                        homegenie.ProgramEngine.SchedulerService.Enable(migCommand.GetOption(0));
                        homegenie.UpdateSchedulerDatabase();
                        break;
                    case "Scheduling.Disable":
                        homegenie.ProgramEngine.SchedulerService.Disable(migCommand.GetOption(0));
                        homegenie.UpdateSchedulerDatabase();
                        break;
                    case "Scheduling.Get":
                        migCommand.Response = JsonConvert.SerializeObject(homegenie.ProgramEngine.SchedulerService.Get(migCommand.GetOption(0)));
                        break;
                    case "Scheduling.List":
                        homegenie.ProgramEngine.SchedulerService.Items.Sort((SchedulerItem s1, SchedulerItem s2) =>
                        {
                            return s1.Name.CompareTo(s2.Name);
                        });
                        migCommand.Response = JsonConvert.SerializeObject(homegenie.ProgramEngine.SchedulerService.Items);
                        break;
                }
            }
            else if (migCommand.Command.StartsWith("Programs."))
            {
                if (migCommand.Command != "Programs.Import")
                {
                    streamcontent = new StreamReader(request.InputStream).ReadToEnd();
                }
                //
                switch (migCommand.Command)
                {
                    case "Programs.Import":
                        string archiveName = "homegenie_program_import.hgx";
                        if (File.Exists(archiveName)) File.Delete(archiveName);
                        //
                        var encoding = (request.Context as HttpListenerContext).Request.ContentEncoding;
                        string boundary = MIG.Gateways.WebServiceUtility.GetBoundary((request.Context as HttpListenerContext).Request.ContentType);
                        MIG.Gateways.WebServiceUtility.SaveFile(encoding, boundary, request.InputStream, archiveName);
                        //
                        var serializer = new XmlSerializer(typeof(ProgramBlock));
                        var reader = new StreamReader(archiveName);
                        newProgram = (ProgramBlock)serializer.Deserialize(reader);
                        reader.Close();
                        //
                        newProgram.Address = homegenie.ProgramEngine.GeneratePid();
                        newProgram.Group = migCommand.GetOption(0);
                        homegenie.ProgramEngine.ProgramAdd(newProgram);
                        //
                        newProgram.IsEnabled = false;
                        newProgram.ScriptErrors = "";
                        newProgram.AppAssembly = null;
                        //
                        homegenie.ProgramEngine.CompileScript(newProgram);
                        //
                        homegenie.UpdateProgramsDatabase();
                        //migCommand.response = JsonHelper.GetSimpleResponse(programblock.Address);
                        migCommand.Response = newProgram.Address.ToString();
                        break;

                    case "Programs.Export":
                        currentProgram = homegenie.ProgramEngine.Programs.Find(p => p.Address == int.Parse(migCommand.GetOption(0)));
                        var writerSettings = new System.Xml.XmlWriterSettings();
                        writerSettings.Indent = true;
                        var programSerializer = new System.Xml.Serialization.XmlSerializer(typeof(ProgramBlock));
                        var builder = new StringBuilder();
                        var writer = System.Xml.XmlWriter.Create(builder, writerSettings);
                        programSerializer.Serialize(writer, currentProgram);
                        writer.Close();
                        migCommand.Response = builder.ToString();
                        //
                        (request.Context as HttpListenerContext).Response.AddHeader("Content-Disposition", "attachment; filename=\"" + currentProgram.Address + "-" + currentProgram.Name.Replace(" ", "_") + ".hgx\"");
                        break;

                    case "Programs.List":
                        var programList = new List<ProgramBlock>(homegenie.ProgramEngine.Programs);
                        programList.Sort(delegate(ProgramBlock p1, ProgramBlock p2)
                        {
                            string c1 = p1.Name + " " + p1.Address;
                            string c2 = p2.Name + " " + p2.Address;
                            return c1.CompareTo(c2);
                        });
                        migCommand.Response = JsonConvert.SerializeObject(programList);
                        break;

                    case "Programs.Add":
                        newProgram = new ProgramBlock() { Group = migCommand.GetOption(0), Name = streamcontent, Type = "Wizard", ScriptCondition = "// A \"return true;\" statement at any point of this code block, will cause the program to run.\n// For manually activated program, just leave a \"return false\" statement here.\n\nreturn false;\n" };
                        newProgram.Address = homegenie.ProgramEngine.GeneratePid();
                        homegenie.ProgramEngine.ProgramAdd(newProgram);
                        homegenie.UpdateProgramsDatabase();
                        migCommand.Response = JsonHelper.GetSimpleResponse(newProgram.Address.ToString());
                        break;

                    case "Programs.Delete":
                        currentProgram = homegenie.ProgramEngine.Programs.Find(p => p.Address == int.Parse(migCommand.GetOption(0)));
                        if (currentProgram != null)
                        {
                            //TODO: remove groups associations as well
                            currentProgram.IsEnabled = false;
                            homegenie.ProgramEngine.ProgramRemove(currentProgram);
                            homegenie.UpdateProgramsDatabase();
                            // remove associated module entry
                            homegenie.Modules.RemoveAll(m => m.Domain == Domains.HomeAutomation_HomeGenie_Automation && m.Address == currentProgram.Address.ToString());
                            homegenie.UpdateModulesDatabase();
                        }
                        break;

                    case "Programs.Compile":
                    case "Programs.Update":
                        newProgram = JsonConvert.DeserializeObject<ProgramBlock>(streamcontent);
                        currentProgram = homegenie.ProgramEngine.Programs.Find(p => p.Address == newProgram.Address);
                        //
                        if (currentProgram == null)
                        {
                            newProgram.Address = homegenie.ProgramEngine.GeneratePid();
                            homegenie.ProgramEngine.ProgramAdd(newProgram);
                        }
                        else
                        {
                            if (currentProgram.Type.ToLower() != newProgram.Type.ToLower())
                            {
                                currentProgram.AppAssembly = null; // dispose assembly and interrupt current task
                            }
                            currentProgram.Type = newProgram.Type;
                            currentProgram.Group = newProgram.Group;
                            currentProgram.Name = newProgram.Name;
                            currentProgram.Description = newProgram.Description;
                            currentProgram.IsEnabled = newProgram.IsEnabled;
                            currentProgram.ScriptCondition = newProgram.ScriptCondition;
                            currentProgram.ScriptSource = newProgram.ScriptSource;
                            currentProgram.Commands = newProgram.Commands;
                            currentProgram.Conditions = newProgram.Conditions;
                            currentProgram.ConditionType = newProgram.ConditionType;
                            // reset last condition evaluation status
                            currentProgram.LastConditionEvaluationResult = false;
                        }
                        //
                        if (migCommand.Command == "Programs.Compile")
                        {
                            // reset previous error status
                            currentProgram.IsEnabled = false;
                            currentProgram.Stop();
                            currentProgram.ScriptErrors = "";
                            //
                            List<ProgramError> errors = homegenie.ProgramEngine.CompileScript(currentProgram);
                            //
                            currentProgram.IsEnabled = newProgram.IsEnabled;
                            currentProgram.ScriptErrors = JsonConvert.SerializeObject(errors);
                            migCommand.Response = currentProgram.ScriptErrors;
                        }
                        //
                        homegenie.UpdateProgramsDatabase();
                        //
                        homegenie.modules_RefreshPrograms();
                        homegenie.modules_RefreshVirtualModules();
                        homegenie.modules_Sort();
                        break;

                    case "Programs.Run":
                        currentProgram = homegenie.ProgramEngine.Programs.Find(p => p.Address == int.Parse(migCommand.GetOption(0)));
                        if (currentProgram != null) {
                            // clear any runtime errors before running
                            currentProgram.ScriptErrors = "";
                            homegenie.ProgramEngine.RaiseProgramModuleEvent(
                                currentProgram,
                                "Runtime.Error",
                                ""
                            );
                            ProgramRun(migCommand.GetOption(0), migCommand.GetOption(1));
                        }
                        break;

                    case "Programs.Toggle":
                        currentProgram = ProgramToggle(migCommand.GetOption(0), migCommand.GetOption(1));
                        break;

                    case "Programs.Break":
                        currentProgram = ProgramBreak(migCommand.GetOption(0));
                        break;

                    case "Programs.Restart":
                        currentProgram = homegenie.ProgramEngine.Programs.Find(p => p.Address == int.Parse(migCommand.GetOption(0)));
                        if (currentProgram != null)
                        {
                            currentProgram.IsEnabled = false;
                            try
                            {
                                currentProgram.Stop();
                            }
                            catch { }
                            currentProgram.IsEnabled = true;
                            homegenie.UpdateProgramsDatabase();
                        }
                        break;

                    case "Programs.Enable":
                        currentProgram = homegenie.ProgramEngine.Programs.Find(p => p.Address == int.Parse(migCommand.GetOption(0)));
                        if (currentProgram != null)
                        {
                            currentProgram.IsEnabled = true;
                            homegenie.UpdateProgramsDatabase();
                        }
                        break;

                    case "Programs.Disable":
                        currentProgram = homegenie.ProgramEngine.Programs.Find(p => p.Address == int.Parse(migCommand.GetOption(0)));
                        if (currentProgram != null)
                        {
                            currentProgram.IsEnabled = false;
                            try
                            {
                                currentProgram.Stop();
                            }
                            catch { }
                            homegenie.UpdateProgramsDatabase();
                        }
                        break;
                }

            }
        }

        internal ProgramBlock ProgramRun(string address, string options)
        {
            int pid = 0; int.TryParse(address, out pid);
            ProgramBlock program = homegenie.ProgramEngine.Programs.Find(p => p.Address == pid);
            if (program != null)
            {
                if (!program.IsEnabled) program.IsEnabled = true;
                try
                {
                    homegenie.ProgramEngine.Run(program, options);
                } catch { };
            }
            return program;
        }

        internal ProgramBlock ProgramToggle(string address, string options)
        {
            int pid = 0; int.TryParse(address, out pid);
            ProgramBlock program = homegenie.ProgramEngine.Programs.Find(p => p.Address == pid);
            if (program != null)
            {
                if (program.IsRunning)
                {
                    ProgramBreak(address);
                    program.IsEnabled = true;
                }
                else
                {
                    if (!program.IsEnabled) program.IsEnabled = true;
                    try
                    {
                        homegenie.ProgramEngine.Run(program, options);
                    } catch { };
                }
            }
            return program;
        }

        internal ProgramBlock ProgramBreak(string address)
        {
            int pid = 0; int.TryParse(address, out pid);
            ProgramBlock program = homegenie.ProgramEngine.Programs.Find(p => p.Address == pid);
            if (program != null)
            {
                program.IsEnabled = false;
                program.Stop();
                homegenie.UpdateProgramsDatabase();
            }
            return program;
        }

    }
}
