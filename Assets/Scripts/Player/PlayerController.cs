﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum E_LOOK_DIRECTION
{
  LEFT = 1,
  BACK = 2,
  RIGHT = 3,
  FRONT = 4
}

public class PlayerController : MonoBehaviour
{
  [Header("Balancing Values")]
  public float speed = 6.0f;
  public float jumpSpeed = 8.0F;
  public float gravity = 20.0F;
  public int Health = 20;
  public float projectileSpeed = 10.0f;
  public float projectileCooldown = 0.5f;
  public float projectileSpawnOffset = 0.6f;
  public float meleeWeaponSpeed = 10.0f;
  public float meleeRange = 2f;
  public float meleeCooldown = 1f;
  public float animationSpeed = 5.0f;
  public float pushPower = 2.0F;
  public int damage = 1;
  public float LaserLength = 6f;
  public LayerMask clickLayerMask;


  [Header("Refs")]
  public CharacterController CharacterController;
  public SpriteRenderer SpriteRenderer;
  public LineRenderer Laser;
  public GameObject DeathAnimPrefab;
  public Sprite[] DirectionalSpritesLeft;
  public Sprite[] DirectionalSpritesRight;
  public Sprite[] DirectionalSpritesFront;
  public Sprite[] DirectionalSpritesBack;
  public GameObject GunLeft;
  public GameObject GunRight;
  public GameObject GunFront;
  public GameObject GunBack;
  public Animator MeleeWeapon;

  [Header("Prefabs")]
  public GameObject ProjectilePrefab;

  // privates
  private Vector3 moveDirection = Vector3.zero;
  private Vector3 lookDirection = new Vector3(0f, 0f, -1f);
  private float spriteAnimIdx = 0;
  private float lastMeleeTime = 0.0f;
  private float lastProjectileTime = 0.0f;
  private int startingHealth = 0;


  public Vector3 LookDirection
  {
    get
    {
      return lookDirection;
    }

    set
    {
      lookDirection = value;
    }
  }

  public int StartingHealth
  {
    get
    {
      return startingHealth;
    }

    set
    {
      startingHealth = value;
    }
  }

  void Start()
  {
    this.startingHealth = Health;
  }

  // Update is called once per frame
  void Update()
  {
    if (Health <= 0) return;

    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
    RaycastHit mouseHit;
    var didHit = Physics.Raycast(ray, out mouseHit, 1000, this.clickLayerMask);
    var heading = didHit ? (mouseHit.point - transform.position) : Vector3.zero;
    var distance = didHit ? heading.magnitude : 0.0f;
    var direction = didHit ? (heading / distance) : Vector3.zero; // This is now the normalized direction.
    direction.y = 0;

    if (didHit)
    {
      Laser.SetPosition(1, transform.position + Vector3.up * 0.5f + direction * LaserLength);

      if (Input.GetButtonDown("Fire1") && Time.time - this.lastProjectileTime > projectileCooldown)
      {
        var curLookDir = this.GetCurrentLookDirection();
        var spawnPos = transform.position + CharacterController.center + direction * projectileSpawnOffset;

        switch (curLookDir)
        {
          case E_LOOK_DIRECTION.BACK:
            spawnPos = GunBack.transform.GetChild(0).transform.position + direction * projectileSpawnOffset;
            break;
          case E_LOOK_DIRECTION.LEFT:
            spawnPos = GunLeft.transform.GetChild(0).transform.position + direction * projectileSpawnOffset;
            break;
          case E_LOOK_DIRECTION.RIGHT:
            spawnPos = GunRight.transform.GetChild(0).transform.position + direction * projectileSpawnOffset;
            break;
          default:
            spawnPos = GunFront.transform.GetChild(0).transform.position + direction * projectileSpawnOffset;
            break;
        }

        var projectile = GameObject.Instantiate(
          ProjectilePrefab,
          spawnPos,
          Quaternion.LookRotation(direction, Vector3.up)
        );
        projectile.GetComponent<Projectile>().Fire(direction, projectileSpeed);
        this.lastProjectileTime = Time.time;
      }
    }

    if (CharacterController.isGrounded)
    {
      moveDirection = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
      this.CalculateLookDirection(direction == Vector3.zero ? moveDirection : direction);

      moveDirection *= speed;
      if (Input.GetButton("Jump"))
        moveDirection.y = jumpSpeed;
    }
    moveDirection.y -= gravity * Time.deltaTime;
    CharacterController.Move(moveDirection * Time.deltaTime);

    // Sprite Animation
    var currentLookDirection = this.GetCurrentLookDirection();
    var currentSprites = DirectionalSpritesFront;
    switch (currentLookDirection)
    {
      case E_LOOK_DIRECTION.BACK:
        currentSprites = DirectionalSpritesBack;
        Laser.SetPosition(0, GunBack.transform.GetChild(0).transform.position);
        GunBack.SetActive(true);
        GunFront.SetActive(false);
        GunRight.SetActive(false);
        GunLeft.SetActive(false);
        break;
      case E_LOOK_DIRECTION.RIGHT:
        currentSprites = DirectionalSpritesRight;
        Laser.SetPosition(0, GunRight.transform.GetChild(0).transform.position);
        GunBack.SetActive(false);
        GunFront.SetActive(false);
        GunRight.SetActive(true);
        GunLeft.SetActive(false);
        break;
      case E_LOOK_DIRECTION.LEFT:
        currentSprites = DirectionalSpritesLeft;
        Laser.SetPosition(0, GunLeft.transform.GetChild(0).transform.position);
        GunBack.SetActive(false);
        GunFront.SetActive(false);
        GunRight.SetActive(false);
        GunLeft.SetActive(true);
        break;
      default:
        currentSprites = DirectionalSpritesFront;
        Laser.SetPosition(0, GunFront.transform.GetChild(0).transform.position);
        GunBack.SetActive(false);
        GunFront.SetActive(true);
        GunRight.SetActive(false);
        GunLeft.SetActive(false);
        break;
    }
    if (Mathf.Abs(moveDirection.x) > 0 || Mathf.Abs(moveDirection.z) > 0)
    {
      spriteAnimIdx += Time.deltaTime * this.animationSpeed;
    }
    else
    {
      spriteAnimIdx = 0;
    }
    SpriteRenderer.sprite = currentSprites[Mathf.FloorToInt(spriteAnimIdx) % currentSprites.Length];
  }

  void OnControllerColliderHit(ControllerColliderHit hit)
  {
    Rigidbody body = hit.collider.attachedRigidbody;
    if (body == null || body.isKinematic)
      return;

    if (hit.moveDirection.y < -0.3F)
      return;

    Vector3 pushDir = new Vector3(hit.moveDirection.x, 0, hit.moveDirection.z);
    body.velocity = pushDir * pushPower;
  }

  void Melee()
  {
    switch (this.GetCurrentLookDirection())
    {
      case E_LOOK_DIRECTION.BACK: MeleeWeapon.SetTrigger("back"); break;
      case E_LOOK_DIRECTION.RIGHT: MeleeWeapon.SetTrigger("right"); break;
      case E_LOOK_DIRECTION.LEFT: MeleeWeapon.SetTrigger("left"); break;
      default: MeleeWeapon.SetTrigger("front"); break;
    }

    // check for enemy-hit
    RaycastHit hit;
    Debug.DrawRay(transform.position, this.LookDirection * this.meleeRange, Color.green, 3, false);
    if (Physics.Raycast(transform.position, this.LookDirection, out hit, this.meleeRange))
    {
      var enemy = hit.collider.GetComponent<Enemy>();
      if (enemy)
      {
        enemy.TakeDamage(damage);
      }
    }
  }

  E_LOOK_DIRECTION GetCurrentLookDirection()
  {
    if (LookDirection.x != 0)
    {
      return LookDirection.x > 0 ? E_LOOK_DIRECTION.RIGHT : E_LOOK_DIRECTION.LEFT;
    }
    else if (LookDirection.z != 0)
    {
      return LookDirection.z > 0 ? E_LOOK_DIRECTION.BACK : E_LOOK_DIRECTION.FRONT;
    }
    // fallback
    return E_LOOK_DIRECTION.FRONT;
  }

  void CalculateLookDirection(Vector3 moveDirection)
  {
    var absHorizontal = Mathf.Abs(moveDirection.x);
    var absVertical = Mathf.Abs(moveDirection.z);

    if (absHorizontal >= absVertical)
    {
      if (moveDirection.x > 0)
      {
        this.lookDirection.x = 1;
        this.lookDirection.z = 0;
      }
      else if (moveDirection.x < 0)
      {
        this.lookDirection.x = -1;
        this.lookDirection.z = 0;
      }
    }
    else
    {
      if (moveDirection.z > 0)
      {
        this.lookDirection.x = 0;
        this.lookDirection.z = 1;
      }
      else if (moveDirection.z < 0)
      {
        this.lookDirection.x = 0;
        this.lookDirection.z = -1;
      }
    }
  }

  public void TakeDamage(int damage = 1)
  {
    this.Health -= damage;
    if (this.Health <= 0)
    {
      this.OnDeath();
    }
  }

  private void OnDeath()
  {
    Camera.main.gameObject.transform.parent = null;

    Instantiate(DeathAnimPrefab, transform.position, Quaternion.identity);

    CharacterController.enabled = false;
    for (var i = 0; i < transform.childCount; i++)
    {
      var curChild = transform.GetChild(i);
      curChild.gameObject.SetActive(false);
    }

    GameManager.instance.OnPlayerDied();
  }
}
