using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public static PlayerController Current; // Diğer silindirlerimin PlayerController sınıfına erişebilmesi için bir değişken. C#'ta static değişkenler herhangi bir sınıf tarafından erişilebilirler.
    public float limitX;       // Karakterin yatay (x) eksenindeki maksimum gidebileceği yolu tutan değişken.
    public float runningSpeed; // Karakterimizin maksimum hızını tutar. Uublic yapmak unity editöründe hızlıca ayarlamayı sağlar
    public float xSpeed;       // Karakterimizin sağa sola ne kadar hızda gideceğinin tutulduğu değişken. X üzerindeki hızı.
    private float _currentRunningSpeed;

    public GameObject ridingCylinderPrefab; // Silindir prefabimi tutan bir gameObject
    public List<RidingCylinder> cylinders; // Ayak altındaki silindirleri tutmak için bir list

    private bool _spawningBridge;
    public GameObject bridgePiecePrefab;
    private BridgeSpawner _bridgeSpawner;
    private float _creatingBridgeTimer;

    private bool _finished;

    private float _scoreTimer = 0;
    public Animator animator;



    public AudioSource cylinderAudioSource, triggerAudioSource;
    public AudioClip gatherAudioClip, dropAudioClip, coinAudioClip;

    private float _dropSoundTimer;
    void Start()
    {
        Current = this;
    }

    void Update()
    {
        if (LevelController.Current == null || !LevelController.Current.gameActive)
        {
            return;
        }
        float newX = 0; // Karakterin x eksenindeki yeni pozisyonu
        float touchXDelta = 0; // Parmağın ya da mouse'un ne kadar sağa sola gittiğini tutan değişken
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Moved) // Ekran dokunulmuş ve dokunulmuş parmak hareket halinde isenin kontrolü
        {
            touchXDelta = Input.GetTouch(0).deltaPosition.x / Screen.width; // Parmağın pozisyon farkı için deltaPosition kullanıyoruz, iyi bi oran için ekran genişliğine bölüyoruz
        }else if (Input.GetMouseButton(0)) // Eğer oyuncu bilgisayardaysa kontrolü
        {
            touchXDelta = Input.GetAxis("Mouse X"); 
        }

        newX = transform.position.x + xSpeed * touchXDelta * Time.deltaTime; // Karakterin x eksenindeki pozisyonu için, Time.deltaTime her karede belirli aşamalarda gitmesi için
        newX = Mathf.Clamp(newX, -limitX, limitX); // x eksenindeki konum sınırlandırılması

        Vector3 newPosition = new Vector3(newX, transform.position.y, transform.position.z + _currentRunningSpeed * Time.deltaTime);
        transform.position = newPosition; // Karakter pozisyonunu yeni pozisyona eşitlemek

        if (_spawningBridge)
        {
            PlayDropSound();
            _creatingBridgeTimer -= Time.deltaTime;
            if (_creatingBridgeTimer < 0)
            {
                _creatingBridgeTimer = 0.01f;
                IncrementCylinderVolume(-0.01f);
                GameObject createdBridgePiece = Instantiate(bridgePiecePrefab);
                Vector3 direction = _bridgeSpawner.endReference.transform.position - _bridgeSpawner.startReference.transform.position;
                float distance = direction.magnitude;
                direction = direction.normalized;
                createdBridgePiece.transform.forward = direction;
                float characterDistance = transform.position.z - _bridgeSpawner.startReference.transform.position.z;
                characterDistance = Mathf.Clamp(characterDistance, 0, distance);
                Vector3 newPiecePosition = _bridgeSpawner.startReference.transform.position + direction * characterDistance;
                newPiecePosition.x = transform.position.x;
                createdBridgePiece.transform.position = newPiecePosition;

                if (_finished)
                {
                    _scoreTimer -= Time.deltaTime;
                    if (_scoreTimer < 0)
                    {
                        _scoreTimer = 0.3f;
                        LevelController.Current.ChangeScore(1);
                    }
                }
            }
        }
    }

    public void ChangeSpeed(float value)
    {
        _currentRunningSpeed = value;
    }

    private void OnTriggerEnter(Collider other) 
    {
        if (other.tag == "AddCylinder")
        {
            cylinderAudioSource.PlayOneShot(gatherAudioClip, 0.1f);
            IncrementCylinderVolume(0.1f);
            Destroy(other.gameObject); // Çarpıştığımız ontrigger objesini yok etmek için
            
        } else if (other.tag == "SpawnBridge")
        {
            StarSpawningBridge(other.transform.parent.GetComponent<BridgeSpawner>());
        } else if (other.tag == "StopSpawnBridge")
        {
            StopSpawningBridge();
            if (_finished)
            {
                LevelController.Current.FinishGame();
            }
        } else if (other.tag == "Finish")
        {
            _finished = true;
            StarSpawningBridge(other.transform.parent.GetComponent<BridgeSpawner>());
        } else if (other.tag == "Coin")
        {
            triggerAudioSource.PlayOneShot(coinAudioClip, 0.1f);
            other.tag = "Untagged";
            LevelController.Current.ChangeScore(10);
            Destroy(other.gameObject);
        }
    }

    private void OnTriggerStay(Collider other) 
    {
        if (LevelController.Current.gameActive)
        {
            if (other.tag == "Trap")
            {
                PlayDropSound();
                IncrementCylinderVolume(-Time.fixedDeltaTime);
            }
        } 
    }

    public void IncrementCylinderVolume(float value)
    {
        if (cylinders.Count == 0) // Listemin eleman sayısı 0 ise ayağımın altında hiç silindir yoktur
        {
            if (value > 0)
            {
                CreateCylinder(value);
            }
            else
            {
               if (_finished)
               {
                   LevelController.Current.FinishGame();
               } 
               else
               {
                   Die();
               } // Game over
            }
        }
        else // Altımda silindir varsa ve yenisi gelsin istiyorsam
        {
                cylinders[cylinders.Count - 1].IncrementCylinderVolume(value); // Son indise ulaşıp en aşağıdaki silindirin boyutunu güncellemek için
        }
    }

    public void Die()
    {
        animator.SetBool("dead", true);
        gameObject.layer = 8;
        Camera.main.transform.SetParent(null);
        LevelController.Current.GameOver();
    }

    public void CreateCylinder(float value)
    {   // Yarattığımız objeyi bir değişkende tutuyoruz. Yaratılan obje karakterimizin child'ı oluyor. Obje içerisinden ridingCylinder component'ını alıyoruz
        RidingCylinder createdCylinder = Instantiate(ridingCylinderPrefab, transform).GetComponent<RidingCylinder>();
        cylinders.Add(createdCylinder);                 // Yarattığım silindiri cylinderler listeme ekliyorum
        createdCylinder.IncrementCylinderVolume(value); // Yarattığım silindirin boyutunu güncelliyorum
    }

    public void DestroyCylinder(RidingCylinder cylinder)
    {
        cylinders.Remove(cylinder); // Silindir listemden çıkarma işlemi
        Destroy(cylinder.gameObject);
    }

    public void StarSpawningBridge(BridgeSpawner spawner)
    {
        _bridgeSpawner = spawner;
        _spawningBridge = true;

    }

    public void StopSpawningBridge()
    {
        _spawningBridge = false;

    }

    public void PlayDropSound(){
        _dropSoundTimer -= Time.deltaTime;
        if (_dropSoundTimer < 0)
        {
            _dropSoundTimer = 0.15f;
            cylinderAudioSource.PlayOneShot(dropAudioClip, 0.1f);
        }
    }
}
