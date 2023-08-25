using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicInstaller.src;

public static class Logger
{
    private static MKUtils.Logger Instance = new MKUtils.Logger();

    public static void Start()
    {
        Instance.Start();
    }

    public static void Start(string filename)
    {
        Instance.Start(filename);
    }

    public static void Write(string message, params object[] args)
    {
        Instance.Write(message, args);
    }

    public static void WriteLine(string message, params object[] args)
    {
        Instance.WriteLine(message, args);
    }

    public static void Error(string message, params object[] args)
    {
        Instance.Error(message, args);
    }

    public static void Warn(string message, params object[] args)
    {
        Instance.Warn(message, args);
    }

    public static void Stop()
    {
        Instance.Stop();
    }
}