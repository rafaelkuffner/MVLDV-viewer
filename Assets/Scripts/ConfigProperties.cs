using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

public class ConfigProperties
{
	public static Dictionary<string, string> load(string filename)
	{
        Dictionary<string, string> properties = new Dictionary<string, string>();
		if (File.Exists(filename))
		{
			List<string> lines = new List<string>(File.ReadAllLines(filename));
			foreach (string line in lines)
			{
                string[] prop = line.Split('=');
                properties.Add(prop[0], prop[1]);
			}
		}
        return properties;
	}
    
}
