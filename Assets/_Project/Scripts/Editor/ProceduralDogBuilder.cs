#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Helper untuk menempatkan anjing DogWalker prosedural ke dalam scene.
/// Menggunakan skrip resmi DogWalker tanpa modifikasi.
/// </summary>
public static class ProceduralDogBuilder
{
    public static GameObject BuildDog(Transform parent, Vector3 position, Quaternion rotation, string dogName = "Milo")
    {
        var dogGo = new GameObject(string.IsNullOrEmpty(dogName) ? "Dog" : $"Dog_{dogName}");
        if (parent != null) dogGo.transform.SetParent(parent);
        position.y = 0f;
        dogGo.transform.position = position;
        dogGo.transform.rotation = rotation;

        var walker = dogGo.AddComponent<DogWalker>();
        walker.createGround = false; // Di scene desa Eldara sudah ada lantai jalan & tanah
        return dogGo;
    }
}
#endif
