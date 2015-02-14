using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Reflection;
using OpenUO.Core;
using Server.Commands;
using Server.Items;

namespace Server
{
    public class WeaponDiceDefaults
    {
        public struct weaponDice
        {
            private Type type;
            private int num;
            private int sides;
            private int offset;

            public Type getType { get { return type; } }
            public int getNum { get { return num; } }
            public int getSides { get { return sides; } }
            public int getOffset { get { return offset; } }

            public weaponDice(Type t, int n, int s, int o)
            {
                type = t;
                num = n;
                sides = s;
                offset = o;
            }
        }

        private static List<weaponDice> dice;

        public static bool HasDice(Type type)
        {
            foreach (weaponDice w in dice)
            {
                if (w.getType == type) return true;
            }
            return false;
        }

        public static weaponDice GetDice(Type type)
        {
            foreach (weaponDice w in dice)
            {
                if (w.getType == type) return w;
            }
            Exception ex = new Exception("Error");
            throw (ex);
        }

        public static void ReplaceDice(Type type, int n, int s, int o)
        {
            try
            {
                weaponDice w = new weaponDice(type, n, s, o);
                for (int i = 0; i < dice.Count; i++)
                {
                    if (dice[i].getType == type)
                    {
                        dice.RemoveAt(i);
                        break;
                    }
                }
                dice.Add(w);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error replacing dice: {0}", ex);
            }
        }

        public static void Initialize()
        {
            CommandSystem.Register("LoadWD", AccessLevel.Administrator, Load_OnCommand);
            CommandSystem.Register("SaveWD", AccessLevel.Administrator, Save_OnCommand);
            LoadWeaponDice();
        }

        [Usage("SaveWD <Filename>")]
        [Description("Saves weapon defaults to the filename supplied.")]
        private static void Save_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            string filename = "";
            if (e.Mobile.AccessLevel >= AccessLevel.Administrator)
            {
                if (e.Arguments.Length >= 1)
                {
                    filename = e.Arguments[0];
                }
                else
                {
                    e.Mobile.SendMessage("Usage:  {0} <Filename>", e.Command);
                    return;
                }

                bool save_ok = true;
                FileStream fs = null;

                try
                {
                    // Create the FileStream to write with.
                    fs = new FileStream(filename, FileMode.Create);
                }
                catch
                {
                    if (from != null)
                    {
                        from.SendMessage("Error creating file {0}", filename);
                    }
                    save_ok = false;
                }

                int count = 0;
                int countWeapons = 0;
                int countNoDice = 0;

                // so far so good
                if (save_ok)
                {
                    // Create the data set
                    DataSet ds = new DataSet("Weapons");

                    try
                    {
                        ds.Tables.Add("Values");
                        ds.Tables["Values"].Columns.Add("Type");
                        ds.Tables["Values"].Columns.Add("Num");
                        ds.Tables["Values"].Columns.Add("Sides");
                        ds.Tables["Values"].Columns.Add("Offset");
                        foreach (Assembly assembly in ScriptCompiler.Assemblies)
                        {
                            foreach (Type type in assembly.GetTypes())
                            {
                                if (!type.Name.Contains("Base") && InheritsFrom(type, typeof (BaseWeapon)))
                                {
                                    countWeapons += 1;
                                    if (HasDice(type))
                                    {
                                        DataRow dr = ds.Tables["Values"].NewRow();
                                        dr["Type"] = type;
                                        dr["Num"] = GetDice(type).getNum;
                                        dr["Sides"] = GetDice(type).getSides;
                                        dr["Offset"] = GetDice(type).getOffset;
                                        ds.Tables["Values"].Rows.Add(dr);
                                        count += 1;
                                    }
                                    else
                                    {
                                        countNoDice += 1;
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (from != null)
                        {
                            from.SendMessage(33, "Error saving values to Dataset: {0}", ex);
                            from.SendMessage(777, "Count was {0} when we stopped.", count);
                        }
                        save_ok = false;
                    }
                    if (save_ok)
                    {
                        try
                        {
                            ds.WriteXml(fs);
                        }
                        catch
                        {
                            if (from != null)
                            {
                                from.SendMessage(33, "Error writing xml file {0}", filename);
                                from.SendMessage(777, "Count was {0} when we stopped.", count);
                            }
                        }
                    }
                }

                try
                {
                    // try to close the file
                    if (fs != null) fs.Close();
                }
                catch
                {
                }

                if (!save_ok && from != null)
                {
                    from.SendMessage("Unable to complete save operation.");
                }

                if (save_ok && from != null)
                {
                    from.SendMessage("Save Complete!");
                    from.SendMessage(777, "Count should be {0}.", count);
                    from.SendMessage(777, "Weapon Count is {0}.", countWeapons);
                    from.SendMessage(777, "Count of Weapons with no Dice is {0}.", countNoDice);
                }
            }
            else
            {
                e.Mobile.SendMessage("You do not have rights to perform this command.");
            }
        }

        /// <summary>
        /// Extension method to check the entire inheritance hierarchy of a
        /// type to see whether the given base type is inherited.
        /// </summary>
        /// <param name="t">The Type object this method was called on</param>
        /// <param name="baseType">The base type to look for in the 
        /// inheritance hierarchy</param>
        /// <returns>True if baseType is found somewhere in the inheritance 
        /// hierarchy, false if not</returns>
        public static bool InheritsFrom(Type t, Type baseType)
        {
            Type cur = t.BaseType;

            while (cur != null)
            {
                if (cur == baseType) return true;

                cur = cur.BaseType;
            }

            return false;
        }

        private static void LoadWeaponDice()
        {
            string filename = "Data/weapondice.xml";

            // Check if the file exists
            if (File.Exists(filename))
            {
                FileStream fs = null;
                try
                {
                    fs = File.Open(filename, FileMode.Open, FileAccess.Read);
                }
                catch
                {
                }

                if (fs == null)
                {
                    Console.WriteLine("Unable to open {0} for loading", filename);
                    return;
                }

                // Create the data set
                DataSet ds = new DataSet("Weapons");

                // Read in the file
                bool fileerror = false;
                try
                {
                    ds.ReadXml(fs);
                }
                catch
                {
                    Console.WriteLine("Error reading xml file {0}", filename);
                    fileerror = true;
                }
                // close the file
                fs.Close();

                if (fileerror)
                {
                    return;
                }

                // Check that at least a single table was loaded
                if (ds.Tables != null && ds.Tables.Count > 0)
                {
                    dice = new List<weaponDice>();
                    foreach (DataRow dr in ds.Tables["Values"].Rows)
                    {
                        try
                        {
                            dice.Add(new weaponDice(ScriptCompiler.FindTypeByName((string)dr["Type"]), Int32.Parse((string)dr["Num"]), Int32.Parse((string)dr["Sides"]),
                                Int32.Parse((string)dr["Offset"])));
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Error inserting values into List<weaponDice> using dice.Add...{0}", ex);
                        }
                    }
                }
            }
            else
                Console.WriteLine("File does not exist.");
        }

        [Usage("LoadWD <Filename>")]
        [Description("Loads weapon defaults as defined in the file supplied.")]
        public static void Load_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            string filename = "";
            if (e.Mobile.AccessLevel >= AccessLevel.Administrator)
            {
                if (e.Arguments.Length >= 1)
                {
                    filename = e.Arguments[0];
                }
                else
                {
                    e.Mobile.SendMessage("Usage:  {0} <Filename>", e.Command);
                    return;
                }
                // Check if the file exists
                if (File.Exists(filename))
                {
                    FileStream fs = null;
                    try
                    {
                        fs = File.Open(filename, FileMode.Open, FileAccess.Read);
                    }
                    catch
                    {
                    }

                    if (fs == null)
                    {
                        if (from != null)
                        {
                            from.SendMessage("Unable to open {0} for loading", filename);
                        }
                        return;
                    }

                    // Create the data set
                    DataSet ds = new DataSet("Weapons");

                    // Read in the file
                    bool fileerror = false;
                    try
                    {
                        ds.ReadXml(fs);
                    }
                    catch
                    {
                        if (from != null)
                        {
                            from.SendMessage(33, "Error reading xml file {0}", filename);
                        }
                        fileerror = true;
                    }
                    // close the file
                    fs.Close();

                    if (fileerror)
                    {
                        return;
                    }
                    int count = 0;

                    // Check that at least a single table was loaded
                    if (ds.Tables != null && ds.Tables.Count > 0)
                    {
                        dice = new List<weaponDice>();
                        foreach (DataRow dr in ds.Tables["Values"].Rows)
                        {
                            try
                            {
                                dice.Add(new weaponDice(ScriptCompiler.FindTypeByName((string)dr["Type"]), Int32.Parse((string)dr["Num"]), Int32.Parse((string)dr["Sides"]),
                                    Int32.Parse((string)dr["Offset"])));
                                count++;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Error inserting values into List<weaponDice> using dice.Add...{0}", ex);
                            }
                        }
                        from.SendMessage("Save Complete!");
                        from.SendMessage(777, "Count should be {0}.", count);
                    }
                }
                else
                    e.Mobile.SendMessage("File does not exist.");
            }
            else
            {
                e.Mobile.SendMessage("You do not have rights to perform this command.");
            }
        }
    }
}
