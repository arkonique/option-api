// 1. On submit, send a /price and /greeks request to the API and display results in the results div. Handle error responses.
// 2. On change in any input field, build the query string for the API requests (both price and greeks) and display it in the query div.

URL = window.location.href.split('?')[0];

document.addEventListener("DOMContentLoaded", async function() {
    // Submit button click handler
    const form = document.getElementById("option-form");
    form.addEventListener("submit", async (e) => {
        const formData = new FormData(form);
        if (!form.checkValidity()) {
            form.reportValidity();
            return;
        }
        e.preventDefault();
        result = sendRequest(formData);
        unpackResult(await result);
    });

    // Input change handler to update query string
    const inputs = form.querySelectorAll("input, select");
    const priceDiv = document.querySelector(".price-content");
    const greeksDiv = document.querySelector(".greeks-content");



});

function asNumber(v) {
  if (v === null || v === undefined || v === "") return null;
  const n = Number(v);
  return Number.isFinite(n) ? n : null;
}

function addIfSet(params, key, value) {
  // preserve zeros; only skip null
  if (value !== null) params.set(key, value);
}

async function sendRequest(formData) {
  const priceUrl = "/price";
  const greeksUrl = "/greeks";

  const S = asNumber(formData.get("S"));
  const K = asNumber(formData.get("K"));
  const T = asNumber(formData.get("T"));
  const r = asNumber(formData.get("r"));
  const q = asNumber(formData.get("q"));
  const sigma = asNumber(formData.get("sigma"));
  const exerciseType = formData.get("exercise") || null;
  const payoffType   = formData.get("payoff") || null;
  const engine       = formData.get("engine") || "auto";
  const steps        = asNumber(formData.get("steps"));
  const paths        = asNumber(formData.get("paths"));
  const basisDegree  = asNumber(formData.get("basisDegree"));

  // log form values
  console.log("Form Values:", {
    S, K, T, r, q, sigma, exerciseType, payoffType, engine, steps, paths, basisDegree
  });

  // Build a common param set
  const baseParams = new URLSearchParams();
  addIfSet(baseParams, "S", S);
  addIfSet(baseParams, "K", K);
  addIfSet(baseParams, "T", T);
  addIfSet(baseParams, "R", r);      // keep keys consistent with your API
  addIfSet(baseParams, "Q", q);
  addIfSet(baseParams, "Sigma", sigma);
  if (exerciseType) baseParams.set("exercise", exerciseType);
  if (payoffType)   baseParams.set("payoff",   payoffType);

  // Greeks first (usually engine/steps/paths aren’t needed for closed-form greeks)
  const greeksParams = new URLSearchParams(baseParams);

  // Price request (may include engine specifics)
  const priceParams = new URLSearchParams(baseParams);
  if (engine)       priceParams.set("engine", engine);
  addIfSet(priceParams, "steps", steps);
  addIfSet(priceParams, "paths", paths);
  addIfSet(priceParams, "basisDegree", basisDegree);

  try {
    const [greeksResp, priceResp] = await Promise.all([
      fetch(`${greeksUrl}?${greeksParams.toString()}`, { method: "GET" }),
      fetch(`${priceUrl}?${priceParams.toString()}`,   { method: "GET" }),
    ]);

    if (!greeksResp.ok){
        // show error message in console. Response comes in the form or { error: "message" }
        const err = await greeksResp.json();
        console.error("Greeks request failed:", err);
    }

    const [greeksResult, priceResult] = await Promise.all([
      greeksResp.json(),
      priceResp.json(),
    ]);

    console.log("Price Result:", priceResult);
    console.log("Greeks Result:", greeksResult);

    return { priceResult, greeksResult };
  } catch (err) {
    console.error(err);
  }
}

function unpackResult(result) {
    error_div = document.getElementById("error-message");
    results_container = document.querySelector("#results-container");
    error_div.innerHTML = "";
    if (!result) return "No result";
    priceResult = result.priceResult;
    greeksResult = result.greeksResult;
    if (priceResult.error) {
        error_div.innerHTML += `<p>Price Error: <span class='error'>${priceResult.error}</span></p>`;
    }
    if (greeksResult.error) {
        error_div.innerHTML += `<p>Greeks Error: <span class='error'>${greeksResult.error}</span></p>`;
    }
    if (!priceResult || !greeksResult) {
        error_div.innerHTML += `<p>Invalid response from server.</p>`;
    }
    priceSpan = results_container.querySelector("#price");
    deltaSpan = results_container.querySelector("#delta");
    gammaSpan = results_container.querySelector("#gamma");
    thetaSpan = results_container.querySelector("#theta");
    vegaSpan  = results_container.querySelector("#vega");
    rhoSpan   = results_container.querySelector("#rho");
    
    priceSpan.innerText = priceResult && priceResult.price !== undefined ? "$ "+priceResult.price.toFixed(2) : "N/A";
    deltaSpan.innerText = greeksResult && greeksResult.delta !== undefined ? greeksResult.delta.toFixed(4) : "N/A";
    gammaSpan.innerText = greeksResult && greeksResult.gamma !== undefined ? greeksResult.gamma.toFixed(4) : "N/A";
    thetaSpan.innerText = greeksResult && greeksResult.theta !== undefined ? greeksResult.theta.toFixed(4) : "N/A";
    vegaSpan.innerText  = greeksResult && greeksResult.vega  !== undefined ? greeksResult.vega.toFixed(4)  : "N/A";
    rhoSpan.innerText   = greeksResult && greeksResult.rho   !== undefined ? greeksResult.rho.toFixed(4)   : "N/A";

}

function httpstring(params) {
    // take params json and return a http query string
    const S = asNumber(params.get("S"));
    const K = asNumber(params.get("K"));
    const T = asNumber(params.get("T"));
    const R = asNumber(params.get("R"));
    const Q = asNumber(params.get("Q"));
    const Sigma = asNumber(params.get("Sigma"));
    const exercise = params.get("exercise");
    const payoff   = params.get("payoff");
    const engine   = params.get("engine") || null;
    const steps    = asNumber(params.get("steps")) || null;
    const paths    = asNumber(params.get("paths")) || null;
    const basisDegree = asNumber(params.get("basisDegree")) || null;

    let str1 = `${URL}/price?S=${S}&K=${K}&T=${T}&R=${R}&Q=${Q}&Sigma=${Sigma}`;
    if (exercise) str += `&exercise=${exercise}`;
    if (payoff)   str += `&payoff=${payoff}`;
    if (engine)   str += `&engine=${engine}`;
    if (steps !== null) str += `&steps=${steps}`;
    if (paths !== null) str += `&paths=${paths}`;
    if (basisDegree !== null) str += `&basisDegree=${basisDegree}`;
    
    let str2 = `/greeks?S=${S}&K=${K}&T=${T}&R=${R}&Q=${Q}&Sigma=${Sigma}`;
    if (exercise) str2 += `&exercise=${exercise}`;
    if (payoff)   str2 += `&payoff=${payoff}`;
    return { price: str1, greeks: str2 };


}

function pythonstring(params) {
    // take params json and return a python requests code snippet
    const S = asNumber(params.get("S"));
    const K = asNumber(params.get("K"));
    const T = asNumber(params.get("T"));
    const R = asNumber(params.get("R"));
    const Q = asNumber(params.get("Q"));
    const Sigma = asNumber(params.get("Sigma"));
    const exercise = params.get("exercise");
    const payoff   = params.get("payoff");
    const engine   = params.get("engine") || null;
    const steps    = asNumber(params.get("steps")) || null;
    const paths    = asNumber(params.get("paths")) || null;
    const basisDegree = asNumber(params.get("basisDegree")) || null;
    let str1 = `import requests

params = {
    "S": ${S},
    "K": ${K},
    "T": ${T},
    "R": ${R},
    "Q": ${Q},
    "Sigma": ${Sigma},`;
    if (exercise) str1 += `\n    "exercise": "${exercise}",`;
    if (payoff)   str1 += `\n    "payoff": "${payoff}",`;
    if (engine)   str1 += `\n    "engine": "${engine}",`;
    if (steps !== null) str1 += `\n    "steps": ${steps},`;
    if (paths !== null) str1 += `\n    "paths": ${paths},`;
    if (basisDegree !== null) str1 += `\n    "basisDegree": ${basisDegree},`;
    str1 += `
}
response = requests.get("${URL}", params=params)
print(response.json())
`;
    let str2 = `import requests
params = {
    "S": ${S},
    "K": ${K},
    "T": ${T},
    "R": ${R},
    "Q": ${Q},
    "Sigma": ${Sigma},`;
    if (exercise) str2 += `\n    "exercise": "${exercise}",`;
    if (payoff)   str2 += `\n    "payoff": "${payoff}",`;
    str2 += `
}
response = requests.get("${URL}", params=params)
print(response.json())
`;
    return { price: str1, greeks: str2 };
}


function updateQueryString() {
    const form = document.getElementById("option-form");
    const formData = new FormData(form);
}

