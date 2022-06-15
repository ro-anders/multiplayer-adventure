using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InitFile : MonoBehaviour {

	// Use this for initialization
	public static byte[] ReadEncryptedKey () {
        byte[] encrypted = { 222, 184, 251, 145, 79, 2, 48, 165, 46, 200, 15, 77, 144, 77, 219, 250, 59, 25, 134, 167, 1, 122, 73, 145, 253, 22, 152, 223, 129, 140, 139, 172, 87, 253, 12, 47, 159, 231, 152, 87, 116, 117, 194, 64, 128, 41, 20, 105 };
        return encrypted;
    }
}
