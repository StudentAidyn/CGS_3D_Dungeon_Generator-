using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;

[ExecuteInEditMode]
public class ThreadRandomiser
{
    private static ThreadRandomiser _instance;

    // Private constructor to prevent instantiation
    private ThreadRandomiser() { }

    public static ThreadRandomiser Instance
    {
        get
        {
            // If the instance is null, create a new instance
            if (_instance == null)
            {
                _instance = new ThreadRandomiser();
            }
            return _instance;
        }
    }

    List<int> RandomNumbers = new List<int>();
    int RandomNumberIndex = 0;

    public void GenerateRandomNumbers(int _totalRandomNumbers = 100)
    {
        RandomNumberIndex = 0;
        RandomNumbers.Clear();
        for(int i = 0; i < _totalRandomNumbers; i++)
        {
            RandomNumbers.Add(UnityEngine.Random.Range(0, int.MaxValue));
        }
    }

    public int GetRandomNumber(int _threadType)
    {
        int randomReturn = RandomNumbers[(RandomNumberIndex + ((RandomNumbers.Count/4) * _threadType)) % RandomNumbers.Count];
        RandomNumberIndex++;
        return randomReturn;
    }

    // Add your singleton-specific methods and properties here
}


/*
     private long stateLow;
    private long stateHigh;

    public ThreadRandomiser()
    {
        stateLow = Thread.CurrentThread.ManagedThreadId.GetHashCode() ^ System.Environment.TickCount;
        stateHigh = stateLow << DateTime.Now.Millisecond * 19 << 13;
    }

    public ThreadRandomiser(ulong seed)
    {
        stateLow = (long)(seed & 0x7FFFFFFF);
        stateHigh = (long)(seed >> 32);
    }

    public float NextFloat()
    {
        const float maxUint = uint.MaxValue;
        return (float)(NextULong() / maxUint);
    }

    public float NextFloat(float minValue, float maxValue)
    {
        if (minValue >= maxValue)
            throw new System.ArgumentOutOfRangeException("minValue must be less than maxValue");

        float range = maxValue - minValue;
        float randomFloat01 = (float)(NextULong() / (float)ulong.MaxValue); // Generate float between 0.0 and 1.0
        return minValue + randomFloat01 * range;
    }

    public ulong NextULong()
    {
        long x;
        long low;
        long high;

        do
        {
            low = stateLow;
            high = stateHigh;

            x = low;
            x ^= (x << 21);
            x ^= (x >> 35);
            x ^= (x << 4);
        } while (Interlocked.CompareExchange(ref stateLow, x, low) != low);

        Interlocked.Exchange(ref stateHigh, high ^ (high << 21));

        return ((ulong)(stateHigh ^ (stateHigh >> 35)) << 32 | (ulong)(x & 0x7FFFFFFF));
    }

    public int Next(int maxValue)
    {
        if (maxValue <= 0)
            throw new System.ArgumentOutOfRangeException("maxValue must be greater than zero");

        return (int)(NextULong() % (ulong)maxValue);
    }

    public int Next(int minValue, int maxValue)
    {
        if (minValue >= maxValue)
            throw new System.ArgumentOutOfRangeException("minValue must be less than maxValue");

        return minValue + Next(maxValue - minValue);
    }
 
 
 */