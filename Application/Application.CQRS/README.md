# CQRS

All commands and queries in this CQRS assembly should obey the following conventions:

- Use `interface` rather than `class` or `record` types
- Do not be prefix with `I` (contrary to everywhere else)
- Suffix with `Command` or `Query` appropriately

```csharp
public interface CreateUserProfileCommand{ /* props */ }    // ✔️ Correct
public interface ICreateUserProfileCommand{ /* props */ }   // ❌ Wrong
public record ICreateUserProfileCommand();                  // ❌ Wrong
public class ICreateUserProfileCommand{ /* props */ }       // ❌ Wrong
```