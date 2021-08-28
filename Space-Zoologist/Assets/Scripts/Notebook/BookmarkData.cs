using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BookmarkData
{
    #region Protected Fields
    // The name of the game object that has a bookmark target component
    protected string targetGameObjectName;
    // Data to set on the target component
    protected object targetComponentData;
    #endregion

    #region Constructors
    public BookmarkData(string targetGameObjectName, object targetComponentData)
    {
        this.targetGameObjectName = targetGameObjectName;
        this.targetComponentData = targetComponentData;
    }
    #endregion

    #region Factory Methods
    public static BookmarkData Create(BookmarkTarget target)
    {
        return new BookmarkData(target.name, target.GetTargetComponentData());
    }
    #endregion

    #region Public Methods
    public void SetTargetData(Dictionary<string, BookmarkTarget> nameTargetMap)
    {
        nameTargetMap[targetGameObjectName].SetTargetComponentData(targetComponentData);
    }
    public override bool Equals(object obj)
    {
        if (obj == null) return false;
        else if (obj.GetType() == GetType()) return this == (BookmarkData)obj;
        else return false;
    }
    public override int GetHashCode()
    {
        return targetGameObjectName.GetHashCode();
    }
    #endregion

    #region Operators
    public static bool operator==(BookmarkData a, BookmarkData b)
    {
        return a.targetGameObjectName == b.targetGameObjectName && a.targetComponentData.Equals(b.targetComponentData);
    }
    public static bool operator!=(BookmarkData a, BookmarkData b)
    {
        return !(a == b);
    }
    #endregion
}
