
// PoC: Denial of Service via Distribution Pool Limit
// Simulates the behavior of `pool_u64` when the maximum shareholder limit is reached.
// If this limit is hit, the `buy_in` function aborts, causing `switch_operator` or `unlock_stake` to fail.

#[derive(Debug, Clone)]
struct Pool {
    limit: usize,
    shareholders: Vec<String>,
}

impl Pool {
    fn create(limit: usize) -> Self {
        Pool {
            limit,
            shareholders: Vec::new(),
        }
    }

    fn buy_in(&mut self, shareholder: &str) -> Result<(), String> {
        if self.shareholders.contains(&shareholder.to_string()) {
            return Ok(());
        }

        if self.shareholders.len() >= self.limit {
            return Err("EMAXIMUM_SHAREHOLDERS_REACHED".to_string());
        }

        self.shareholders.push(shareholder.to_string());
        Ok(())
    }
}

fn main() {
    println!("--- Verification: Staker Lockout via Distribution Limit ---");

    // Aptos const: MAXIMUM_PENDING_DISTRIBUTIONS = 20
    let max_distributions = 20;
    let mut distribution_pool = Pool::create(max_distributions);

    println!("Step 1: Fill the distribution pool with {} pending operators.", max_distributions);

    for i in 0..max_distributions {
        let operator = format!("Operator_{}", i);
        match distribution_pool.buy_in(&operator) {
            Ok(_) => println!("Switched to {}, pending commission added.", operator),
            Err(e) => println!("Failed to add {}: {}", operator, e),
        }
    }

    println!("Current pending distributions: {}", distribution_pool.shareholders.len());

    println!("Step 2: Attempt to switch to the 21st Operator.");
    let new_operator = "Operator_21";

    // This simulates `switch_operator` failing because it tries to add a new shareholder (Op_21) to the pool
    match distribution_pool.buy_in(new_operator) {
        Ok(_) => println!("[FAILED] Pool accepted more than limit."),
        Err(e) => {
            println!("[VERIFIED] Transaction Aborted: {}", e);
            println!("CRITICAL: Staker is now unable to switch operators or unlock stake until pending distributions are cleared.");
            println!("If the lockup period is long, the staker is effectively frozen.");
        }
    }
}
