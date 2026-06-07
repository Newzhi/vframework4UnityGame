using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbstractResource
{
    //引用计数
    private int Ref;

    T Load<T>()
    {
        T t = default;
        Ref++;
        return t;
    }

    void Unload()
    {
        Ref--;
    }
}
