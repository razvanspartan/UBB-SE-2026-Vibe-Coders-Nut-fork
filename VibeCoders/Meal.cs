using System;
using System.Collections.Generic;

public class Meal
{
    //Atributes
    private string _name;
    private List<string> _ingredients;
    private string _instructions;

    //Constructor
    public Meal()
    {
        _ingredients = new List<string>();
    }

    //(Getters and Setters)
    public string Name
    {
        get { return _name; }
        set { _name = value; }
    }

    public List<string> Ingredients
    {
        get { return _ingredients; }
        set { _ingredients = value; }
    }

    public string Instructions
    {
        get { return _instructions; }
        set { _instructions = value; }
    }

    
}

