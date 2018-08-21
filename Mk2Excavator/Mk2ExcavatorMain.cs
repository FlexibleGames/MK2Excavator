using System;
using System.Reflection;
using System.IO;
using UnityEngine;

class Mk2ExcavatorMain : FortressCraftMod
{
    public ushort mExcavatorCubeType;
    private string XMLConfigFile = "Mk2ExcavatorConfig.XML";
    private string XMLConfigPath = "";
    private string XMLModID = "FlexibleGames.Mk2Excavator";
    private int XMLModVersion = 10;
    public Mk2ExcavatorConfig mConfig;
    private bool mXMLFileExists;

    public override ModRegistrationData Register()
    {
        XMLConfigPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        Debug.Log("Mk2Excavator AssemblyPath: " + XMLConfigPath);
      
        //XMLConfigPath = ModManager.GetModPath();
        //string text = Path.Combine(XMLConfigPath, XMLModID + Path.DirectorySeparatorChar + XMLModVersion);

        string configfile = Path.Combine(XMLConfigPath, XMLConfigFile);

        //text += String.Concat(Path.DirectorySeparatorChar + XMLConfigFile);

        Debug.Log("Mk2Excavator: Checking if XMLConfig File Exists at " + configfile);

        if (File.Exists(configfile))
        {
            mXMLFileExists = true;
            Debug.Log("Mk2Excavator: XMLConfig File Exists, loading.");
            string xmltext = File.ReadAllText(configfile);
            try
            {
                mConfig = (Mk2ExcavatorConfig)XMLParser.DeserializeObject(xmltext, typeof(Mk2ExcavatorConfig));
                // catch insane values, clamp them to lesser insane values
                if (mConfig.DigHeight < 4) mConfig.DigHeight = 4;
                if (mConfig.DigRadius < 1) mConfig.DigRadius = 1;
                if (mConfig.PowerPerBlockDefault < 1) mConfig.PowerPerBlockDefault = 1;
                if (mConfig.PowerPerBlockOre < 1) mConfig.PowerPerBlockOre = 1;
                if (mConfig.DigHeight > 2048) mConfig.DigHeight = 2048;
                if (mConfig.DigRadius > 1024) mConfig.DigRadius = 1024;
                if (mConfig.PowerPerBlockDefault > 10000) mConfig.PowerPerBlockDefault = 10000;
                if (mConfig.PowerPerBlockOre > 40000) mConfig.PowerPerBlockOre = 40000;
                if (mConfig.MaxPower > 100000) mConfig.MaxPower = 100000;
                if (mConfig.OPBlock > 20) mConfig.OPBlock = 20;
                if (mConfig.OPBlock < 2) mConfig.OPBlock = 1;
            }
            catch (Exception e)
            {
                Debug.LogError("Mk2Excavator: Something is wrong with ConfigXML, using defaults.\n Exception: " + e.ToString());
                mXMLFileExists = false;
            }
            Debug.Log("Mk2Excavator: XMLConfig File Loaded.");            
        }
        else
        {
            Debug.LogWarning("Mk2Excavator: ERROR: XML File Does not exist at " + configfile);
            mXMLFileExists = false;
        }           

        ModRegistrationData lRegData = new ModRegistrationData();
        lRegData.RegisterEntityHandler("FlexibleGames.Mk2Excavator");
        TerrainDataEntry ltde;
        TerrainDataValueEntry ltdve;
        global::TerrainData.GetCubeByKey("FlexibleGames.Mk2Excavator", out ltde, out ltdve);
        bool flag = ltde != null;
        if (flag)
        {
            this.mExcavatorCubeType = ltde.CubeType;        
        }
        UIManager.NetworkCommandFunctions.Add("FlexibleGames.Mk2ExcavatorWindow", new UIManager.HandleNetworkCommand(Mk2ExcavatorWindow.HandleNetworkCommand));

        return lRegData;
    }

    public override ModCreateSegmentEntityResults CreateSegmentEntity(ModCreateSegmentEntityParameters parameters)
    {
        ModCreateSegmentEntityResults lcser = new ModCreateSegmentEntityResults();
        bool flag = parameters.Cube == this.mExcavatorCubeType;
        if (flag)
        {
            if (mXMLFileExists)
            {
                lcser.Entity = new Mk2Excavator(parameters.Segment, parameters.X, parameters.Y, parameters.Z, parameters.Cube, parameters.Flags, parameters.Value, parameters.LoadFromDisk, mConfig.PowerPerBlockDefault, mConfig.PowerPerBlockOre, mConfig.DigRadius, mConfig.DigHeight, mConfig.MaxPower, mConfig.OPBlock);
            }
            else
            {
                Debug.LogWarning("Mk2Excavator: ERROR: XMLConfig File Does not exist, using defaults.");
                lcser.Entity = new Mk2Excavator(parameters.Segment, parameters.X, parameters.Y, parameters.Z, parameters.Cube, parameters.Flags, parameters.Value, parameters.LoadFromDisk, 20, 80, 9, 128, 1280, 5);
            }
        }
        return lcser;
    }
}

