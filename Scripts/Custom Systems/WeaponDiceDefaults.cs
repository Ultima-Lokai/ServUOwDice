using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using Server.Commands;
using Server.Items;

namespace Server
{
    public class WeaponDiceDefaults
    {
		private static bool AUTO_LOAD_DEFAULTS = true; // Default dice values are loaded on Server startup.
		private static bool AUTO_SAVE_DEFAULTS = true; // Default dice values are saved during World Save.
		private static string DEFAULT_DICE_XML = "Data/weapondice.xml";
        private static AccessLevel DEFAULT_ACCESS_LEVEL = AccessLevel.Administrator;
		
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
			// This method is meant to return an existing value. It should ALWAYS be called in a Try block so that
			//    invalid results are blocked.
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

        private static List<Type> baseTypes;

        public static void Initialize()
        {
            CommandSystem.Register("LoadWD", DEFAULT_ACCESS_LEVEL, LoadDice_OnCommand);
            CommandSystem.Register("SaveWD", DEFAULT_ACCESS_LEVEL, SaveDice_OnCommand);
            Console.WriteLine("Found {0} Base Types.", FindBaseTypes());
            if (AUTO_LOAD_DEFAULTS) LoadRoutine(null, DEFAULT_DICE_XML);
			EventSink.WorldSave += new WorldSaveEventHandler(SaveWeaponDice);
        }

        private static void SaveRoutine(Mobile from, string filename)
        {
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
                            // We do not save 'Base' types like BaseKnife, etc. but we save anything else that inherits from BaseWeapon
                            if (InheritsFrom(type, typeof(BaseWeapon)) && !baseTypes.Contains(type))
                            {
                                countWeapons += 1;
                                if (HasDice(type))
                                {
                                    // Create a new Data Row
                                    DataRow dr = ds.Tables["Values"].NewRow();

                                    // Populate the Row
                                    dr["Type"] = type;
                                    dr["Num"] = GetDice(type).getNum;
                                    dr["Sides"] = GetDice(type).getSides;
                                    dr["Offset"] = GetDice(type).getOffset;

                                    // Add the Row to the DataSet
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
                    if (from == null)
                    {
                        Console.WriteLine("Error saving values to Dataset: {0}", ex);
                        Console.WriteLine("Count was {0} when we stopped.", count);
                    }
                    else
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
                        if (from == null)
                        {
                            Console.WriteLine("Error writing xml file {0}", filename);
                            Console.WriteLine("Count was {0} when we stopped.", count);
                        }
                        else
                        {
                            from.SendMessage(33, "Error writing xml file {0}", filename);
                            from.SendMessage(777, "Count was {0} when we stopped.", count);
                        }
                        save_ok = false;
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

            if (from == null)
            {
                if (save_ok)
                {
                    Console.WriteLine("Save Complete!");
                    Console.WriteLine("Count should be {0}.", count);
                    Console.WriteLine("Total Weapon Count is {0}.", countWeapons);
                    Console.WriteLine("Count of Weapons with no Dice is {0}.", countNoDice);
                }
                else
                {
                    Console.WriteLine("Unable to complete save operation.");
                }
            }
            else
            {
                if (save_ok)
                {
                    from.SendMessage("Save Complete!");
                    from.SendMessage(777, "Count should be {0}.", count);
                    from.SendMessage(777, "Total Weapon Count is {0}.", countWeapons);
                    from.SendMessage(777, "Count of Weapons with no Dice is {0}.", countNoDice);
                }
                else
                {
                    from.SendMessage("Unable to complete save operation.");
                }
            }
        }

        [Usage("SaveWD <Filename>")]
        [Description("Saves weapon defaults to the filename supplied.")]
        private static void SaveDice_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            if (from == null) return;

            if (from.AccessLevel < DEFAULT_ACCESS_LEVEL)
            {
                from.SendMessage("You do not have rights to perform this command.");
            }
            else
            {
                if (e.Arguments.Length >= 1)
                {
                    SaveRoutine(from, e.Arguments[0]);
                }
                else
                {
                    from.SendMessage("Usage:  {0} <Filename>", e.Command);
                }
            }
        }

        private static void SaveWeaponDice(WorldSaveEventArgs e)
        {
            if (AUTO_SAVE_DEFAULTS) SaveRoutine(null, DEFAULT_DICE_XML);
		}

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

        public static int FindBaseTypes()
        {
            baseTypes = new List<Type>();
            ConstructorInfo ctor;

            foreach (Assembly assembly in ScriptCompiler.Assemblies)
            {
                foreach (Type type in assembly.GetTypes())
                {
                    if (InheritsFrom(type, typeof(BaseWeapon)))
                    {
                        ctor = type.GetConstructor(Type.EmptyTypes);
                        if (ctor == null)
                            baseTypes.Add(type);
                    }
                }
            }

            return baseTypes.Count;
        }

        private static void LoadRoutine(Mobile from, string filename)
        {
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
                int errorcount = 0;

                // Check that at least a single table was loaded
                if (ds.Tables != null && ds.Tables.Count > 0)
                {
                    dice = new List<weaponDice>();
                    foreach (DataRow dr in ds.Tables["Values"].Rows)
                    {
                        try
                        {
                            string typestring = (string) dr["Type"];
                            Type type = ScriptCompiler.FindTypeByName(typestring);
                            if (type == null) type = ScriptCompiler.FindTypeByFullName(typestring);
                            dice.Add(new weaponDice(type, Int32.Parse((string) dr["Num"]),
                                Int32.Parse((string) dr["Sides"]),
                                Int32.Parse((string) dr["Offset"])));
                            count++;
                        }
                        catch
                        {
                            errorcount++;
                        }
                    }
                    if (from == null)
                    {
                        Console.WriteLine("Load of weapon dice values Complete!");
                        Console.WriteLine("Count should be {0}.", count);
                        Console.WriteLine("Error Count was {0}.", errorcount);
                    }
                    else
                    {
                        from.SendMessage("Load of weapon dice values Complete!");
                        from.SendMessage(777, "Count should be {0}.", count);
                        from.SendMessage(777, "Error Count was {0}.", errorcount);
                    }
                }
            }
            else
            {
                if (from == null)
                {
                    Console.WriteLine("Weapon Dice default file does not exist: {0}", filename);
                }
                else
                {
                    from.SendMessage("File does not exist: {0}", filename);
                }
            }
        }

        [Usage("LoadWD <Filename>")]
        [Description("Loads weapon defaults as defined in the file supplied.")]
        public static void LoadDice_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            if (from == null) return;

            if (from.AccessLevel < DEFAULT_ACCESS_LEVEL)
            {
                from.SendMessage("You do not have rights to perform this command.");
            }
            else
            {
                if (e.Arguments.Length >= 1)
                {
                    LoadRoutine(from, e.Arguments[0]);
                }
                else
                {
                    from.SendMessage("Usage:  {0} <Filename>", e.Command);
                }
            }
        }
    }
}
