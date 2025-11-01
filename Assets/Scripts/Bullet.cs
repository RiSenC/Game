using UnityEngine;

public class Bullet : MonoBehaviour
{
    public int damage = 10;
    public float lifetime = 3f;
    
    void Start()
    {
        // Автоуничтожение через заданное время
        Destroy(gameObject, lifetime);
    }
    
    void OnTriggerEnter2D(Collider2D collision)
    {
        // Пуля уничтожается при столкновении с чем угодно, кроме игрока и других пуль
        if (!collision.CompareTag("Player") && !collision.CompareTag("Bullet"))
        {
            // Здесь позже добавим урон врагам
            Destroy(gameObject);
        }
    }
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        // Альтернативный вариант для не-триггер коллайдеров
        if (!collision.gameObject.CompareTag("Player") && !collision.gameObject.CompareTag("Bullet"))
        {
            Destroy(gameObject);
        }
    }
}