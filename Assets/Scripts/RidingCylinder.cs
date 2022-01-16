using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RidingCylinder : MonoBehaviour
{
    private bool _filled; // Silindirin en büyük haline gelip gelmediğini tutan değer
    private float _value; // Silindirin sayısal olarak ne kadar dolduğunu tutan değer
    
    public void IncrementCylinderVolume(float value)  // Silindirin boyutunun artmasını ve azalmasını sağlayan bir fonksiyon 
    {
        _value += value;
        if (_value > 1)
        {
            float leftValue = _value - 1; // 1'den kalan değer
            int cylinderCount = PlayerController.Current.cylinders.Count;
            transform.localPosition = new Vector3(transform.localPosition.x, -0.5f * (cylinderCount - 1) - 0.25f, transform.localPosition.z); // Silindirin boyutunu tam olarak 1 yap
            transform.localScale = new Vector3(0.5f, transform.localScale.y , 0.5f);
            PlayerController.Current.CreateCylinder(leftValue); // 1'den ne kadar büyükse o kadar büyüklükte yeni bir silindir yarat
        }else if (_value < 0)
        {
            PlayerController.Current.DestroyCylinder(this); // Karakterimize bu silindiri yok etmesini söyleyeceğiz
        } else
        {
            // Silindirin boyutunu güncelleyeceğiz
            int cylinderCount = PlayerController.Current.cylinders.Count;
            transform.localPosition = new Vector3(transform.localPosition.x, -0.5f * (cylinderCount - 1) - 0.25f * _value, transform.localPosition.z); // Silindirin boyutunu tam olarak 1 yap
            transform.localScale = new Vector3(0.5f * _value, transform.localScale.y , 0.5f * _value);
        }
    }
}
