using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class ThreadRandomiser
{
    private static ThreadRandomiser _instance;

    public static ThreadRandomiser Instance
    {
        get
        {
            if (_instance == null) _instance = new ThreadRandomiser();
            return _instance;
        }
    }

    private ThreadRandomiser()
    {
        GenerateRandomNumbers();
        //the constructor is private so that you can't instantiate it
    }

    // Variables 
    public const int TOTAL_RANDOM_NUMBERS = 100;
    public int m_randomNumberIndex = 0;
    public List<int> m_randomNumbers = new List<int>();



    // Methods
    // Generates random numbers and adds it to the list of random numbers
    public void GenerateRandomNumbers(int TotalRandomNumbers = TOTAL_RANDOM_NUMBERS)
    {
        m_randomNumberIndex = 0;
        m_randomNumbers.Clear();
        for (int i = 0; i < TotalRandomNumbers; i++)
        {
            m_randomNumbers.Add(UnityEngine.Random.Range(0, Int32.MaxValue));
        }
    }

    public int GetRandomNumber()
    {
        int number = m_randomNumbers[m_randomNumberIndex % 100];
        m_randomNumberIndex++;
        return number;
    }

}