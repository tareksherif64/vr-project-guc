using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CustomerManager : MonoBehaviour
{
    public List<Customer> customers = new List<Customer>();
    public int currentCustomer = 0;
    public string EvidenceScene; // set in Inspector

    void Start()
    {
        if (customers.Count > 0)
            customers[0].StartCustomer();
    }

    public void NextCustomer()
    {
        currentCustomer++;
        if (currentCustomer < customers.Count)
        {
            customers[currentCustomer].StartCustomer();
        }
        else
        {
            Debug.Log("All customers finished");
            SceneManager.LoadScene(EvidenceScene);
        }
    }
}