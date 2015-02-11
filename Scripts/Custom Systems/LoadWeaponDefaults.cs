using System;
using System.Collections.Generic;
using System.Data;
using Server.Mobiles;
using Server.Items;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Server.Commands;

namespace Server
{
    public class LoadWeaponDefaults
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

        public static weaponDice GetDice(Type type)
        {
            foreach (weaponDice w in dice)
            {
                if (w.getType == type) return w;
            }
            return new weaponDice(null, 0, 0, 0);
        }

        public static void Initialize()
        {
            CommandSystem.Register("LoadWD", AccessLevel.Administrator, Load_OnCommand);
            EventSink.Movement += new MovementEventHandler(EventSink_SayRandomStuffCheck);
            LoadWeaponDice();
        }

        private static void LoadRandomStuff()
        {
            try
            {

            }
            catch
            {
            }
        }

        private static void EventSink_SayRandomStuffCheck(MovementEventArgs e)
        {
            if (e.Mobile is PlayerMobile)
            {
                PlayerMobile from = (PlayerMobile) e.Mobile;
                foreach (Mobile mobile in from.GetMobilesInRange(8))
                {
                    if (mobile is PlayerMobile)
                    {
                        if (Utility.RandomMinMax(1, 16) > 13)
                        {

                        }
                    }
                }
            }
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
                    e.Mobile.SendMessage("File does not exist.");
            }
            else
            {
                e.Mobile.SendMessage("You do not have rights to perform this command.");
            }
        }
    }
}
