using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bookmark
{
    // Public accessors
    public string Label => label;

    protected string label;
    protected BookmarkData[] datas = new BookmarkData[0];

    #region Constructors
    public Bookmark(string label, params BookmarkData[] datas)
    {
        this.label = label;
        this.datas = datas;
    }
    #endregion

    #region Public Methods
    public void Navigate(Dictionary<string, BookmarkTarget> nameTargetMap)
    {
        foreach (BookmarkData data in datas)
        {
            data.SetTargetData(nameTargetMap);
        }
    }
    public override bool Equals(object obj)
    {
        if (obj == null) return false;
        else if (obj.GetType() == GetType()) return this == (Bookmark)obj;
        else return false;
    }
    public override int GetHashCode()
    {
        return datas.GetHashCode();
    }
    #endregion

    #region Operators
    public static bool operator ==(Bookmark a, Bookmark b)
    {
        return a.datas.SequenceEqual(b.datas);
    }
    public static bool operator !=(Bookmark a, Bookmark b)
    {
        return !(a == b);
    }
    #endregion
}
