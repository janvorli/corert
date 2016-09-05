// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

// TODO: Move these tests to CoreFX once they can be run on CoreRT

internal static class MiniStress
{
    private const int Pass = 100;

    static TimeSpan DefaultTestDuration = new TimeSpan(0, 1, 0); //TimeSpan(48, 0, 0);

    public static bool IsDone = false;
    
    public static int Main()
    {
        StartTestOnSeparateThread(new ThreadStart(GcTest.RunGcTest));
        
        Stopwatch sw = Stopwatch.StartNew();
        TimeSpan lastMessage = TimeSpan.FromTicks(0);
        TimeSpan elapsed;

        do
        {
            Thread.Sleep(1000);
            elapsed = sw.Elapsed;
            if ((elapsed - lastMessage) > new TimeSpan(0, 0, 5))
            {
                Console.WriteLine("Time left: {0})", (DefaultTestDuration - elapsed));
                lastMessage = elapsed;
            }
        }
        while (elapsed < DefaultTestDuration);
        
        IsDone = true;
        
        return Pass;
    }
    
    public static void StartTestOnSeparateThread(ThreadStart testEntry)
    {
        Thread thread;

        thread = new Thread(new ParameterizedThreadStart(MiniStress.ExecuteTest));
        thread.Start(testEntry);
    }

    static void ExecuteTest(object context)
    {
        ThreadStart testEntry = (ThreadStart)context;

        while (!IsDone)
        {
            testEntry.Invoke();
        }
    }

    public static void Fail(string message)
    {
        Console.WriteLine("\n\n\n====================\n\n" +
            String.Format("Error!\n    {0}\n", message) +
            "\n====================\n\n\n");
        throw new Exception(message);
    }    
}

class GcTest
{
    const int ItemCount = 256;

    public static byte[] s_array1;
    static byte[] s_array2;
    static string[] s_stringArray;
    static long s_bytesAllocated = 0;
    
    static Random s_random = new Random();

    public static void RunGcTest()
    {
        byte[] array3;
        byte[] array4;
        Thread[] threads;
        int threadCount = 4;
        long lastReportedAllocation = 0;
        long currentAllocation;

        Console.WriteLine("Staring simple GC test...");

        threads = new Thread[threadCount];
        for (int t = 0; t < threadCount; t++)
        {
            threads[t] = new Thread(new ThreadStart(GcTest.Test2));
            threads[t].Start();
        }

        for (int i = 0; i < 1000000; i++)
        {
            s_array1 = new byte[ItemCount];
            s_array2 = new byte[ItemCount];
            array3 = new byte[ItemCount];
            array4 = new byte[ItemCount];

            currentAllocation = Interlocked.Add(ref s_bytesAllocated, 4 * ItemCount);
            if ((currentAllocation - lastReportedAllocation) > 800000000)
            {
                Console.WriteLine("Total bytes allocated: {0}", currentAllocation);
                lastReportedAllocation = currentAllocation;
            }
        }

        for (int t = 0; t < threadCount; t++)
        {
            threads[t].Join();
        }

        Console.WriteLine("Finished simple GC test.");
    }

    static void Test2()
    {
        byte[] array3;
        byte[] array4;
        string[] stringArray;
        
        stringArray = new string[8192];
        for (int j = 0; j < stringArray.Length; j++)
        {
            stringArray[j] = new string('a', s_random.Next(1, 8192));
        }

        for (int i = 0; i < 1000000; i++)
        {
            s_array1 = new byte[ItemCount];
            s_array2 = new byte[ItemCount];
            array3 = new byte[ItemCount];
            array4 = new byte[ItemCount];

            Interlocked.Add(ref s_bytesAllocated, 4 * ItemCount);
            /*long value = Interlocked.Add(ref s_bytesAllocated, 4 * ItemCount);
            if ((i % 100) == 0)
            {
                value = value & 0xFFFFF;
                for (long j = 0; j < value; j++); // spining to cause GC injection
            }*/

            s_stringArray = stringArray;
        }
    }
} 
