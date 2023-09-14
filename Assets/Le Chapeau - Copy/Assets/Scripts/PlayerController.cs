using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class PlayerController : MonoBehaviourPunCallbacks, IPunObservable
{

    [HideInInspector]
    public int id;

    [Header("Info")]
    public float moveSpeed;
    public float jumpForce;
    public GameObject hatObject;
    public GameObject playerMesh;
    public AudioClip bing;

    [HideInInspector]
    public float curHatTime;

    [Header("Components")]
    public Rigidbody rig;
    public Player photonPlayer;
    private Vector3 moveDir = Vector3.zero;
    public Material op1;
    public Material op2;
    public Material op3;

    [PunRPC]
    public void Initialize(Player player)
    {
        photonPlayer = player;
        id = player.ActorNumber;

        GameManager.instance.players[id - 1] = this;

        //give the first player the hat
        if (id == 1)
            GameManager.instance.GiveHat(id, true);

        if (!photonView.IsMine)
            rig.isKinematic = true;

        if (id == 2)
            GetComponent<Renderer>().material = op1;
        if (id == 3)
            GetComponent<Renderer>().material = op2;
        if (id == 4)
            GetComponent<Renderer>().material = op3;
    }

    // Update is called once per frame
    void Update()
    {
        
        Move();

        if (Input.GetKeyDown(KeyCode.Space))
            TryJump();

        // track the amount of time we're wearing the hat
        if (hatObject.activeInHierarchy)
            curHatTime += Time.deltaTime;

        // the host will check if the player has won
        if (PhotonNetwork.IsMasterClient)
        {
            if (curHatTime >= GameManager.instance.timeToWin && !GameManager.instance.gameEnded)
            {
                GameManager.instance.gameEnded = true;
                GameManager.instance.photonView.RPC("WinGame", RpcTarget.All, id);
            }
        }
    }

    void Move()
    {
        float x = Input.GetAxis("Horizontal") * moveSpeed;
        float z = Input.GetAxis("Vertical") * moveSpeed;

        rig.velocity = new Vector3(x, rig.velocity.y, z);

        moveDir = rig.velocity;
        // Apply the new position to the Rigidbody using transform.position
    }
    void TryJump()
    {
        Ray ray = new Ray(transform.position, Vector3.down);

        if (Physics.Raycast(ray, 0.7f))
            rig.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }


    //sets they players hat to ative or not
    public void SetHat (bool hasHat)
    {
        playerMesh.SetActive(false);
        hatObject.SetActive(hasHat);
        AudioSource audio = GetComponent<AudioSource>();
        audio.clip = bing;
        audio.Play();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!photonView.IsMine)
            return;

        if (collision.gameObject.CompareTag("Player"))
        {
            if (GameManager.instance.GetPlayer(collision.gameObject).id == GameManager.instance.playerWithHat)
            {
                if (GameManager.instance.CanGetHat())
                {
                    GameManager.instance.photonView.RPC("GiveHat", RpcTarget.All, id, false);
                }
            }
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(curHatTime);
        }
        else if (stream.IsReading)
        {
            curHatTime = (float)stream.ReceiveNext();
        }
    }
}
