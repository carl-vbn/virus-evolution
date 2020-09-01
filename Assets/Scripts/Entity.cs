using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.AI;

public class Entity : MonoBehaviour
{
    public bool permanentlyInfected = false;
    public bool drawTargetDirection = false;
    public float movementAreaSize = 100;
    public float movementEnergyCost = 0.1F;
    public float movementSpeedDamageThreshold;
    public House home;
    public Material healthyMaterial;
    public Material infectedMaterial;
    public Material deadMaterial;
    public Text txtInfectionState;
    public Slider healthBar;
    public Slider energyBar;
    public ParticleSystem coughParticles;
    public GameObject deathParticles;

    public Dictionary<string, float> BaseTraits {get; private set;}
    public Virus Infection {get; private set;}
    public EntityState CurrentState {get; private set;}
    public float Health {get; private set;}
    public float Energy {get; private set;}

    public static string[] TRAIT_KEYS = new string[]{"cough_rate", "sweating", "virus_resistance", "aggressiveness", "movement_speed", "exhaustion_tolerance", "pain_tolerance"};

    private Rigidbody rb;
    private NavMeshAgent navMeshAgent;
    private int timeSinceInfection;
    private Vector3 targetPosition;
    private Transform movingTarget;
    private RectTransform canvasTransform;
    private CanvasGroup canvasGroup;
    private Camera cam;
    private MeshRenderer meshRenderer;
    private bool outdatedPath;

    private HashSet<string> immunity;

    // Anti-stuck system
    private Vector3 lastPosition;
    private int stuckCounter;

    // Start is called before the first frame update
    void Start()
    {
        BaseTraits = new Dictionary<string, float>();
        foreach (string traitName in TRAIT_KEYS) {
            BaseTraits.Add(traitName, 1F);
        }

        rb = GetComponent<Rigidbody>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        meshRenderer = GetComponent<MeshRenderer>();
        timeSinceInfection = 0;
        Health = 100;
        Energy = Random.Range(90, 150); // 150 is above the max but it's there to avoid that every entity runs out of energy at the same time
        immunity = new HashSet<string>();
        cam = Camera.main;
        CurrentState = EntityState.Wandering;

        Canvas canvas = GetComponentInChildren<Canvas>();
        canvasTransform = canvas.GetComponent<RectTransform>();
        canvasGroup = canvas.GetComponent<CanvasGroup>();

        InvokeRepeating("Tick", 1F, 1F);

        targetPosition = NewTarget();
        lastPosition = transform.position;

        outdatedPath = true;

        txtInfectionState.text = "Healthy ["+CurrentState+"]";
    }

    void Tick() {
        if (Infection != null) {
            timeSinceInfection++;

            if (!permanentlyInfected && Random.value < 0.01F/GetTraitValue("virus_resistance")) {
                immunity.Add(Infection.ToString());
                SetInfection(null);
                navMeshAgent.speed = GetTraitValue("movement_speed")*3.5F;
            }
        }

        if (!IsInside() && lastPosition == transform.position) {
            stuckCounter++;

            if (stuckCounter > 5) {
                if (CurrentState == EntityState.Going_Home && Vector3.Distance(transform.position, home.transform.position) < 10F) {
                    EnterHome();
                }

                movingTarget = null;
                targetPosition = NewTarget();
            }
        } else {
            stuckCounter = 0;
        }

        lastPosition = transform.position;

        if (!IsInside() && outdatedPath && navMeshAgent.isOnNavMesh) {
            navMeshAgent.SetDestination(targetPosition);
            outdatedPath = false;
        }

        if (!IsInside() && Random.value < GetTraitValue("cough_rate") * 0.05F) {
            Energy -= 3;
            Cough();
        }
    }

    void Update() {
        if (Health <= 0) return;

        if (CurrentState == EntityState.At_Home || CurrentState == EntityState.At_Hospital) {
            Energy += Time.deltaTime * 10;

            if (CurrentState == EntityState.At_Hospital)
                Health += Time.deltaTime * 2;
            else if (Health < 85)
                Health += Time.deltaTime;

            if (CurrentState == EntityState.At_Home && Energy > 100 && Health > 85) {
                Energy = 100;
                Health = Mathf.Min(Health, 100);
                SetInside(false);
                targetPosition = NewTarget();
            } else if (CurrentState == EntityState.At_Hospital && Health > 100 && Energy > 100) {
                Energy = 100;
                Health = 100;
                SetInside(false);
                targetPosition = NewTarget();

                if (Infection != null && Random.value < 0.75F) SetInfection(null);
            }
        } else {
            float immunityEnergyCost = 0.4F * GetTraitValue("virus_resistance");
            Energy -= (navMeshAgent.velocity.magnitude * movementEnergyCost + immunityEnergyCost) * Time.deltaTime;

            if (navMeshAgent.velocity.magnitude > movementSpeedDamageThreshold) {
                Health -= 5F * Time.deltaTime;
            }

            if (Energy < 0) {
                Health -= 1.25F * Time.deltaTime;
            }

            if (Health < 50F - ((GetTraitValue("pain_tolerance")-1) * 25F)) {
                Vector3 closestHospital = ClosestHospital();
                if (CurrentState != EntityState.Going_Home || Vector3.Distance(transform.position, home.transform.position) > Vector3.Distance(transform.position, closestHospital)) {
                    CurrentState = EntityState.Going_To_Hospital;
                    targetPosition = closestHospital;
                    outdatedPath = true;
                }
            }

            if ((CurrentState == EntityState.Wandering || CurrentState == EntityState.Chasing) && (Energy < 40F - ((GetTraitValue("exhaustion_tolerance")-1) * 20) || Health < 75F - ((GetTraitValue("pain_tolerance")-1) * 40F))) {
                CurrentState = EntityState.Going_Home;
                targetPosition = home.transform.position;
                outdatedPath = true;
            }


            if (Vector3.Distance(transform.position, targetPosition) < 0.6F) {
                if (CurrentState == EntityState.Going_Home) {
                    EnterHome();
                } else if (CurrentState == EntityState.Going_To_Hospital) {
                    SetInside(true);
                    CurrentState = EntityState.At_Hospital;
                } else {
                    targetPosition = NewTarget();
                }
            }
        }

        if (drawTargetDirection) Debug.DrawLine(transform.position, targetPosition, Color.blue);

        if (CurrentState == EntityState.Chasing) {
            if (movingTarget != null) {
                targetPosition = movingTarget.position;
                outdatedPath = true;
            } else {
                CurrentState = EntityState.Wandering;
            }
        } 

        Vector3 fwd = Camera.main.transform.forward;
        canvasTransform.rotation = Quaternion.LookRotation(fwd);

        if (!IsInside()) {
            float distanceFromCamera = Vector3.Distance(transform.position, cam.transform.position);
            float opacity = Mathf.Min(1, Mathf.Max(0, -0.5F*distanceFromCamera+10));
            canvasGroup.alpha = opacity;
            if (opacity > 0.01F) {
                healthBar.value = Health/100F;
                energyBar.value = Energy/100F;
            }
        } else {
            canvasGroup.alpha = 0;
        }

        if (Health <= 0) {
            Die();
        }
    }

    void EnterHome() {
        SetInside(true);
        if (Infection != null) home.SpreadInfection(this, Infection);

        CurrentState = EntityState.At_Home;
    }

    void Cough() {
        coughParticles.Play();
        if (Infection != null) {
            foreach (GameObject entity in GameObject.FindGameObjectsWithTag("Entity")) {
                if (Vector3.Distance(entity.transform.position, transform.position) < 4F) {
                    Debug.DrawLine(entity.transform.position, transform.position, Color.cyan);
                    Infect(entity.GetComponent<Entity>());       
                }
            }
        }
    }

    // Used to make entities disappear when entering their home or an hospital
    void SetInside(bool inside) {
        if (inside) {
            outdatedPath = false;
        }

        navMeshAgent.enabled = !inside;
        GetComponent<Collider>().enabled = !inside;
        meshRenderer.enabled = !inside;
    }

    bool IsInside() {
        return CurrentState == EntityState.At_Home || CurrentState == EntityState.At_Hospital;
    }

    Vector3 ClosestHospital() {
        GameObject closestHospital = null;
        foreach (GameObject hospital in GameObject.FindGameObjectsWithTag("HospitalEntry")) {
            if (closestHospital == null || Vector3.Distance(transform.position, hospital.transform.position) < Vector3.Distance(transform.position, closestHospital.transform.position)) {
                closestHospital = hospital;
            }
        }

        return closestHospital.transform.position;
    }

    Vector3 NewTarget() {
        outdatedPath = true;
        if (Random.value < (GetTraitValue("aggressiveness")-1)*0.5F) {
            Entity closestHealthy = null;
            foreach (GameObject entityGO in GameObject.FindGameObjectsWithTag("Entity")) {
                Entity entity = entityGO.GetComponent<Entity>();
                if (entityGO == gameObject || entity.IsInside() || entity.Infection != null) continue;

                if (closestHealthy == null || Vector3.Distance(transform.position, entity.transform.position) < Vector3.Distance(transform.position, closestHealthy.transform.position)) {
                    closestHealthy = entity;
                }
            }
            if (closestHealthy != null && Mathf.Abs(closestHealthy.transform.position.x) <= movementAreaSize && Mathf.Abs(closestHealthy.transform.position.z) <= movementAreaSize) {
                movingTarget = closestHealthy.transform;
                CurrentState = EntityState.Chasing;
                return movingTarget.position;
            } else {
                CurrentState = EntityState.Wandering;
                movingTarget = null;
                return RandomPositionOnMap();
            }
        } else {
            CurrentState = EntityState.Wandering;
            movingTarget = null;
            return RandomPositionOnMap();
        }
    }

    Vector3 RandomPositionOnMap() {
        Vector3 randomDirection = Random.insideUnitSphere * movementAreaSize;
        NavMeshHit hit;
        NavMesh.SamplePosition(randomDirection, out hit, movementAreaSize, 1);
        return hit.position;
    }

    public float GetTraitValue(string name) {
        if (BaseTraits.ContainsKey(name)) {
            if (Infection != null && Infection.HostTraitMultipliers.ContainsKey(name)) {
                return BaseTraits[name] * Infection.HostTraitMultipliers[name];
            } else {
                return BaseTraits[name];
            }
        } else {
            return 1;
        }
    }

    public void SetInfection(Virus Infection) {
        this.Infection = Infection;

        if (Infection != null) {
            meshRenderer.material = infectedMaterial;
            txtInfectionState.text = "Infected ["+CurrentState+"]";
            timeSinceInfection = 0;

            navMeshAgent.speed = 3.5F + (GetTraitValue("movement_speed")-1);
        } else {
            meshRenderer.material = healthyMaterial;
            txtInfectionState.text = "Healthy ["+CurrentState+"]";
        }

        if (CurrentState == EntityState.Chasing) {
            targetPosition = NewTarget();
        }
    }

    public bool IsImmuneTo(Virus infection) {
        if (infection == null) return true;
        return immunity.Contains(infection.ToString());
    }

    void Die() {
        CancelInvoke("Tick");
        Health = 0;
        meshRenderer.material = deadMaterial;
        navMeshAgent.enabled = false;
        rb.freezeRotation = false;
        rb.isKinematic = true;
        canvasGroup.alpha = 0;

        StartCoroutine(DeathAnimation());

        Debug.DrawLine(transform.position, transform.position+new Vector3(0, 10, 0), Color.red, 1000F);
    }

    IEnumerator DeathAnimation() {
        for (float rot = 0; rot<97; rot+=3F) {
            transform.Rotate(new Vector3(0,0,3F));
            yield return new WaitForSeconds(0.025F);
        }

        GameObject instance = Instantiate(deathParticles);
        instance.transform.position = transform.position - new Vector3(0F, 2F, 0F);
        instance.GetComponent<ParticleSystem>().Play();
        Destroy(instance, 3F);
        Destroy(gameObject, 0.25F);
    }

    public void Infect(Entity other) {
        if (Infection == null)
            throw new System.Exception("Infection cannot be null");
        
        if (other.Infection == null && !other.IsImmuneTo(Infection)) {
            other.SetInfection(this.Infection.CreateCopy(0.1F, 1F));
        }
    }

    void OnTriggerEnter(Collider collider) {
        if (BaseTraits == null) return; // Collision detected before Start() was called

        if (!IsInside() && collider.gameObject.CompareTag("Entity")) {
            Entity collidedEntity = collider.gameObject.GetComponent<Entity>();
            if (CurrentState == EntityState.Chasing && movingTarget == collider.transform) {
                collidedEntity.Health -= 25; // Attack causes damage
                if (Infection != null) Infect(collidedEntity); // 100% infection chance when attacking
            } else if (Infection != null && Random.value < GetTraitValue("sweating")*0.33F) {
                Infect(collidedEntity);
            }

            if (CurrentState == EntityState.Wandering || CurrentState == EntityState.Chasing) targetPosition = NewTarget();
        }
    }
}

public enum EntityState {
    At_Home, At_Hospital, Wandering, Chasing, Going_Home, Going_To_Hospital
}